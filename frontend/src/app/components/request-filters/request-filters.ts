import { Component, output } from '@angular/core';
import { EMPTY_FILTERS, RequestFilterValues } from '../../models/request-filter-values.model';
import { PRIORITY_LABELS, REQUEST_PRIORITIES } from '../../models/request-priority.model';
import { REQUEST_STATUSES, STATUS_LABELS } from '../../models/request-status.model';

/**
 * Presentational: the search and filter fields.
 * Reports every change; debouncing and the server call are handled by the state service.
 */
@Component({
  selector: 'app-request-filters',
  templateUrl: './request-filters.html',
  styleUrl: './request-filters.css'
})
export class RequestFilters {
  readonly filtersChange = output<RequestFilterValues>();

  readonly statuses = REQUEST_STATUSES;
  readonly statusLabels = STATUS_LABELS;
  readonly priorities = REQUEST_PRIORITIES;
  readonly priorityLabels = PRIORITY_LABELS;

  private values: RequestFilterValues = { ...EMPTY_FILTERS };

  update(field: keyof RequestFilterValues, event: Event): void {
    const value = (event.target as HTMLInputElement | HTMLSelectElement).value;
    this.values = { ...this.values, [field]: value } as RequestFilterValues;
    this.filtersChange.emit(this.values);
  }

  clear(form: HTMLFormElement): void {
    form.reset();
    this.values = { ...EMPTY_FILTERS };
    this.filtersChange.emit(this.values);
  }
}