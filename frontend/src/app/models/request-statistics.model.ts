import { RequestPriority } from './request-priority.model';
import { RequestStatus } from './request-status.model';

// Mirrors RequestStatisticsDto (the two aggregations).
export interface RequestStatistics {
  totalCount: number;
  byStatus: Record<RequestStatus, number>;
  byPriority: Record<RequestPriority, number>;
}