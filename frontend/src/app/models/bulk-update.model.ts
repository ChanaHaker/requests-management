import { RequestStatus } from './request-status.model';

// Mirrors BulkUpdateItem: one request and the version the user is looking at.
export interface BulkUpdateItem {
  id: number;
  rowVersion: string;
}

// Mirrors StatusChangeOutcome.
export type BulkUpdateOutcome = 'Updated' | 'NotFound' | 'Conflict' | 'InvalidTransition';

export const BULK_OUTCOME_LABELS: Record<BulkUpdateOutcome, string> = {
  Updated: 'עודכנה',
  NotFound: 'לא נמצאה',
  Conflict: 'עודכנה בינתיים על ידי משתמש אחר',
  InvalidTransition: 'מעבר סטטוס לא מותר'
};

// Mirrors BulkUpdateItemResult.
export interface BulkUpdateItemResult {
  id: number;
  outcome: BulkUpdateOutcome;
  message: string | null;
  newRowVersion: string | null;
}

// Mirrors BulkUpdateStatusResult.
export interface BulkUpdateResult {
  requested: number;
  updated: number;
  failed: number;
  results: BulkUpdateItemResult[];
}

// What the bulk actions bar reports.
export interface BulkUpdateCommand {
  newStatus: RequestStatus;
  userName: string;
}