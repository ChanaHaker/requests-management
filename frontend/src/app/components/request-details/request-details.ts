import { DatePipe } from '@angular/common';
import { Component, computed, effect, input, output, signal } from '@angular/core';
import { RequestHistoryEntry } from '../../models/request-history-entry.model';
import { RequestItem } from '../../models/request-item.model';
import { PRIORITY_LABELS } from '../../models/request-priority.model';
import { ALLOWED_TRANSITIONS, RequestStatus, STATUS_LABELS } from '../../models/request-status.model';
import { UiMessage } from '../../models/ui-message.model';
import { DEFAULT_USER_NAME, isValidUserName } from '../../models/user-name.model';

/**
 * Presentational: details of the selected request, status change form and change history.
 * Reports the user's choice; the state service sends it to the server and handles 409.
 */
@Component({
  selector: 'app-request-details',
  imports: [DatePipe],
  templateUrl: './request-details.html',
  styleUrl: './request-details.css'
})
export class RequestDetails {
  readonly request = input.required<RequestItem>();
  readonly history = input<RequestHistoryEntry[]>([]);
  readonly historyLoading = input(false);
  readonly saving = input(false);
  readonly message = input<UiMessage | null>(null);

  readonly changeStatus = output<{ newStatus: RequestStatus; userName: string }>();
  readonly close = output<void>();

  readonly statusLabels = STATUS_LABELS;
  readonly priorityLabels = PRIORITY_LABELS;

  readonly newStatus = signal<RequestStatus | ''>('');
  readonly userName = signal(DEFAULT_USER_NAME);

  // Only transitions allowed from the current status are offered (the server still enforces them).
  readonly allowedTargets = computed(() => ALLOWED_TRANSITIONS[this.request().status]);
  readonly userNameValid = computed(() => isValidUserName(this.userName()));
  readonly canSubmit = computed(() => this.newStatus() !== '' && this.userNameValid() && !this.saving());

  constructor() {
    // When the request changes (another row, a successful update, or a reload after 409),
    // the previous choice may no longer be valid, so it is cleared.
    effect(() => {
      this.request();
      this.newStatus.set('');
    });
  }

  onStatusSelected(event: Event): void {
    this.newStatus.set((event.target as HTMLSelectElement).value as RequestStatus | '');
  }

  onUserNameInput(event: Event): void {
    this.userName.set((event.target as HTMLInputElement).value);
  }

  submit(): void {
    const newStatus = this.newStatus();
    if (newStatus === '' || !this.canSubmit()) {
      return;
    }
    this.changeStatus.emit({ newStatus, userName: this.userName().trim() });
  }
}