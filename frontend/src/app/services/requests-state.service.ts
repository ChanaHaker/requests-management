import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { catchError, of, switchMap, tap } from 'rxjs';
import { PagedResult } from '../models/paged-result.model';
import { RequestItem } from '../models/request-item.model';
import { RequestQuery } from '../models/request-query.model';
import { RequestStatistics } from '../models/request-statistics.model';
import { SortField } from '../models/sort.model';
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
  private readonly selectedIdSignal = signal<number | null>(null);

  // Read-only views for the components.
  readonly query = this.querySignal.asReadonly();
  readonly page = this.pageSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly statistics = this.statisticsSignal.asReadonly();
  readonly selectedId = this.selectedIdSignal.asReadonly();

  constructor() {
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

  loadStatistics(): void {
    this.api.getStatistics().subscribe({
      next: statistics => this.statisticsSignal.set(statistics),
      error: () => this.statisticsSignal.set(null)
    });
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

  select(item: RequestItem): void {
    this.selectedIdSignal.set(item.id);
  }

  // A new object with the same values still counts as a change, so the request is sent again.
  reload(): void {
    this.querySignal.update(q => ({ ...q }));
  }
}