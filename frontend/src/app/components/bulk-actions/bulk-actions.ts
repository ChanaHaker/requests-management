import { Component, computed, input, output, signal } from '@angular/core';
import { BULK_OUTCOME_LABELS, BulkUpdateCommand, BulkUpdateResult } from '../../models/bulk-update.model';
import { REQUEST_STATUSES, RequestStatus, STATUS_LABELS } from '../../models/request-status.model';
import { DEFAULT_USER_NAME, isValidUserName } from '../../models/user-name.model';

/**
 * Presentational: the bulk update bar (shown when rows are checked) and the result of the last bulk update.
 * Reports the user's choice; the state service sends it to the server.
 */
@Component({
  selector: 'app-bulk-actions',
  templateUrl: './bulk-actions.html',
  styleUrl: './bulk-actions.css'
})
export class BulkActions {
  readonly checkedCount = input(0);
  readonly saving = input(false);
  readonly result = input<BulkUpdateResult | null>(null);
  readonly error = input<string | null>(null);

  readonly apply = output<BulkUpdateCommand>();
  readonly clear = output<void>();

  // All statuses are offered: the checked rows may have different statuses,
  // and the server reports an InvalidTransition for each row that cannot move.
  readonly statuses = REQUEST_STATUSES;
  readonly statusLabels = STATUS_LABELS;
  readonly outcomeLabels = BULK_OUTCOME_LABELS;

  readonly newStatus = signal<RequestStatus | ''>('');
  readonly userName = signal(DEFAULT_USER_NAME);

  readonly userNameValid = computed(() => isValidUserName(this.userName()));
  readonly canApply = computed(
    () => this.checkedCount() > 0 && this.newStatus() !== '' && this.userNameValid() && !this.saving()
  );
  readonly failures = computed(() => this.result()?.results.filter(r => r.outcome !== 'Updated') ?? []);

  onStatusSelected(event: Event): void {
    this.newStatus.set((event.target as HTMLSelectElement).value as RequestStatus | '');
  }

  onUserNameInput(event: Event): void {
    this.userName.set((event.target as HTMLInputElement).value);
  }

  submit(): void {
    const newStatus = this.newStatus();
    if (newStatus === '' || !this.canApply()) {
      return;
    }
    this.apply.emit({ newStatus, userName: this.userName().trim() });
  }
}