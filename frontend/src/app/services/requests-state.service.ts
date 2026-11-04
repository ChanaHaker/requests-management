import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { Subject, catchError, debounceTime, distinctUntilChanged, of, switchMap, tap } from 'rxjs';
import { PagedResult } from '../models/paged-result.model';
import { RequestFilterValues } from '../models/request-filter-values.model';
import { RequestHistoryEntry } from '../models/request-history-entry.model';
import { RequestItem } from '../models/request-item.model';
import { RequestQuery } from '../models/request-query.model';
import { RequestStatistics } from '../models/request-statistics.model';
import { RequestStatus } from '../models/request-status.model';
import { SortField } from '../models/sort.model';
import { UiMessage } from '../models/ui-message.model';
import { toErrorMessage } from './http-error';
import { RequestsApiService } from './requests-api.service';

const DEFAULT_QUERY: RequestQuery = {
  page: 1,
  pageSize: 20,
  sortBy: 'CreatedAt',
  sortDirection: 'Desc',
  status: null,
  priority: null,
  organizationName: '',
  assignedTo: '',
  createdFrom: '',
  createdTo: '',
  search: ''
};

// Wait for the user to stop typing before querying the server.
const FILTER_DEBOUNCE_MS = 400;

const CONFLICT_MESSAGE =
  'הפנייה עודכנה בינתיים על ידי משתמש אחר. הנתונים העדכניים נטענו מהשרת - בדקו ונסו שוב.';

/**
 * State of the requests screen. Components read the signals and call the methods;
 * they never call the API or use RxJS themselves.
 */
@Injectable({ providedIn: 'root' })
export class RequestsStateService {
  private readonly api = inject(RequestsApiService);

  // Writable state - changed only inside this service.
  private readonly querySignal = signal<RequestQuery>(DEFAULT_QUERY);
  private readonly pageSignal = signal<PagedResult<RequestItem> | null>(null);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<string | null>(null);
  private readonly statisticsSignal = signal<RequestStatistics | null>(null);

  private readonly selectedSignal = signal<RequestItem | null>(null);
  private readonly historySignal = signal<RequestHistoryEntry[]>([]);
  private readonly historyLoadingSignal = signal(false);
  private readonly savingSignal = signal(false);
  private readonly messageSignal = signal<UiMessage | null>(null);

  private readonly filterChanges = new Subject<RequestFilterValues>();

  // Read-only views for the components.
  readonly query = this.querySignal.asReadonly();
  readonly page = this.pageSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly statistics = this.statisticsSignal.asReadonly();
  readonly selected = this.selectedSignal.asReadonly();
  readonly selectedId = computed(() => this.selectedSignal()?.id ?? null);
  readonly history = this.historySignal.asReadonly();
  readonly historyLoading = this.historyLoadingSignal.asReadonly();
  readonly saving = this.savingSignal.asReadonly();
  readonly message = this.messageSignal.asReadonly();

  constructor() {
    // Filters: debounce typing, ignore unchanged values, then start again from page 1.
    this.filterChanges
      .pipe(
        debounceTime(FILTER_DEBOUNCE_MS),
        distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
        takeUntilDestroyed()
      )
      .subscribe(filters =>
        this.querySignal.update(q => ({
          ...q,
          page: 1,
          search: filters.search,
          status: filters.status || null,
          priority: filters.priority || null,
          organizationName: filters.organizationName,
          assignedTo: filters.assignedTo,
          createdFrom: filters.createdFrom,
          createdTo: filters.createdTo
        }))
      );

    // Every query change triggers a server call.
    toObservable(this.querySignal)
      .pipe(
        tap(() => {
          this.loadingSignal.set(true);
          this.errorSignal.set(null);
        }),
        // switchMap cancels the previous HTTP request when a newer query arrives,
        // so a slow old response can never overwrite a newer one.
        switchMap(query =>
          this.api.getRequests(query).pipe(
            catchError((error: HttpErrorResponse) => {
              this.errorSignal.set(toErrorMessage(error));
              return of(null);
            })
          )
        ),
        takeUntilDestroyed()
      )
      .subscribe(result => {
        this.loadingSignal.set(false);
        if (result) {
          this.pageSignal.set(result);
        }
      });

    this.loadStatistics();
  }

  // ---------- List ----------

  loadStatistics(): void {
    this.api.getStatistics().subscribe({
      next: statistics => this.statisticsSignal.set(statistics),
      error: () => this.statisticsSignal.set(null)
    });
  }

  setFilters(filters: RequestFilterValues): void {
    this.filterChanges.next(filters);
  }

  // Same field: toggle direction. New field: start ascending. Always back to page 1.
  setSort(field: SortField): void {
    this.querySignal.update(q => ({
      ...q,
      page: 1,
      sortBy: field,
      sortDirection: q.sortBy === field && q.sortDirection === 'Asc' ? 'Desc' : 'Asc'
    }));
  }

  setPage(page: number): void {
    this.querySignal.update(q => ({ ...q, page }));
  }

  setPageSize(pageSize: number): void {
    this.querySignal.update(q => ({ ...q, page: 1, pageSize }));
  }

  // A new object with the same values still counts as a change, so the request is sent again.
  reload(): void {
    this.querySignal.update(q => ({ ...q }));
  }

  // ---------- Selected request ----------

  select(item: RequestItem): void {
    this.selectedSignal.set(item);
    this.messageSignal.set(null);
    this.loadHistory(item.id);
  }

  clearSelection(): void {
    this.selectedSignal.set(null);
    this.historySignal.set([]);
    this.messageSignal.set(null);
  }

  updateStatus(newStatus: RequestStatus, userName: string): void {
    const current = this.selectedSignal();
    if (!current) {
      return;
    }

    this.savingSignal.set(true);
    this.messageSignal.set(null);

    // Sends the RowVersion the user is looking at. The server rejects it with 409 if it is stale.
    this.api.updateStatus(current.id, newStatus, current.rowVersion, userName).subscribe({
      next: updated => {
        this.savingSignal.set(false);
        this.selectedSignal.set(updated);
        this.replaceInPage(updated);
        this.messageSignal.set({ kind: 'success', text: 'הסטטוס עודכן בהצלחה.' });
        this.loadHistory(updated.id);
        this.loadStatistics();
      },
      error: (error: HttpErrorResponse) => {
        this.savingSignal.set(false);

        if (error.status === 409) {
          // Someone else changed the request: show the latest version instead of overwriting it.
          this.messageSignal.set({ kind: 'error', text: CONFLICT_MESSAGE });
          this.refreshSelected(current.id);
          this.reload();
          this.loadStatistics();
        } else {
          this.messageSignal.set({ kind: 'error', text: toErrorMessage(error) });
        }
      }
    });
  }

  private refreshSelected(id: number): void {
    this.api.getById(id).subscribe({
      next: latest => {
        this.selectedSignal.set(latest);
        this.loadHistory(id);
      },
      error: (error: HttpErrorResponse) =>
        this.messageSignal.set({ kind: 'error', text: toErrorMessage(error) })
    });
  }

  private loadHistory(id: number): void {
    this.historyLoadingSignal.set(true);
    this.api.getHistory(id).subscribe({
      next: history => {
        this.historyLoadingSignal.set(false);
        this.historySignal.set(history);
      },
      error: (error: HttpErrorResponse) => {
        this.historyLoadingSignal.set(false);
        this.historySignal.set([]);
        this.messageSignal.set({ kind: 'error', text: toErrorMessage(error) });
      }
    });
  }

  // Shows the updated row (new status and RowVersion) without reloading the whole page.
  private replaceInPage(updated: RequestItem): void {
    this.pageSignal.update(page =>
      page ? { ...page, items: page.items.map(item => (item.id === updated.id ? updated : item)) } : page
    );
  }
}