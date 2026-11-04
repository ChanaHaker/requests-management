import { RequestStatus } from './request-status.model';

// Mirrors RequestStatusHistoryDto (one audit row).
export interface RequestHistoryEntry {
  id: number;
  requestId: number;
  previousStatus: RequestStatus;
  newStatus: RequestStatus;
  changedAt: string;
  changedBy: string;
}