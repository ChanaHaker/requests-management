import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { PagedResult } from '../../models/paged-result.model';
import { RequestItem } from '../../models/request-item.model';
import { PRIORITY_LABELS } from '../../models/request-priority.model';
import { STATUS_LABELS } from '../../models/request-status.model';
import { SortDirection, SortField } from '../../models/sort.model';

/**
 * Presentational: shows one page of requests with sortable headers and paging controls.
 * It does not sort or page by itself - it reports the user's choice and the server does the work.
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

  readonly sortChange = output<SortField>();
  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();
  readonly selectRequest = output<RequestItem>();

  readonly statusLabels = STATUS_LABELS;
  readonly priorityLabels = PRIORITY_LABELS;
  readonly pageSizes = [10, 20, 50, 100];

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