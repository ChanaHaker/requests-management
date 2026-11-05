import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
import { PagedResult } from '../../models/paged-result.model';
import { RequestItem } from '../../models/request-item.model';
import { PRIORITY_LABELS } from '../../models/request-priority.model';
import { STATUS_LABELS } from '../../models/request-status.model';
import { SortDirection, SortField } from '../../models/sort.model';

/**
 * Presentational: shows one page of requests with sortable headers, paging controls
 * and checkboxes for bulk updates. It does not sort, page or update by itself -
 * it reports the user's choice and the state service asks the server.
 */
@Component({
  selector: 'app-requests-table',
  imports: [DatePipe, DecimalPipe],
  templateUrl: './requests-table.html',
  styleUrl: './requests-table.css'
})
export class RequestsTable {
  readonly result = input.required<PagedResult<RequestItem>>();
  readonly sortBy = input.required<SortField>();
  readonly sortDirection = input.required<SortDirection>();
  readonly selectedId = input<number | null>(null);
  readonly checkedIds = input<ReadonlySet<number>>(new Set());

  readonly sortChange = output<SortField>();
  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();
  readonly selectRequest = output<RequestItem>();
  readonly toggleChecked = output<number>();
  readonly toggleAll = output<void>();

  readonly statusLabels = STATUS_LABELS;
  readonly priorityLabels = PRIORITY_LABELS;
  readonly pageSizes = [10, 20, 50, 100];

  readonly allChecked = computed(() => {
    const items = this.result().items;
    const checked = this.checkedIds();
    return items.length > 0 && items.every(item => checked.has(item.id));
  });

  sortIndicator(field: SortField): string {
    if (this.sortBy() !== field) {
      return '';
    }
    return this.sortDirection() === 'Asc' ? '▲' : '▼';
  }

  onPageSizeSelected(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    this.pageSizeChange.emit(value);
  }
}