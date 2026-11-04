import { RequestPriority } from './request-priority.model';
import { RequestStatus } from './request-status.model';
import { SortDirection, SortField } from './sort.model';

// Mirrors RequestQueryParameters: paging, sorting and filters sent to the server.
export interface RequestQuery {
  page: number;
  pageSize: number;
  sortBy: SortField;
  sortDirection: SortDirection;
  status: RequestStatus | null;
  priority: RequestPriority | null;
  organizationName: string;
  assignedTo: string;
  createdFrom: string;
  createdTo: string;
  search: string;
}