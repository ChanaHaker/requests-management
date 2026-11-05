import { DecimalPipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { PRIORITY_LABELS, REQUEST_PRIORITIES } from '../../models/request-priority.model';
import { REQUEST_STATUSES, STATUS_LABELS } from '../../models/request-status.model';
import { RequestStatistics as Statistics } from '../../models/request-statistics.model';

/**
 * Presentational: displays the two aggregations (by status and by priority),
 * with loading and error states. Receives the data from the page; does not call the API itself.
 */
@Component({
  selector: 'app-request-statistics',
  imports: [DecimalPipe],
  templateUrl: './request-statistics.html',
  styleUrl: './request-statistics.css'
})
export class RequestStatistics {
  readonly statistics = input<Statistics | null>(null);
  readonly loading = input(false);
  readonly error = input<string | null>(null);

  readonly retry = output<void>();

  readonly statuses = REQUEST_STATUSES;
  readonly statusLabels = STATUS_LABELS;
  readonly priorities = REQUEST_PRIORITIES;
  readonly priorityLabels = PRIORITY_LABELS;
}