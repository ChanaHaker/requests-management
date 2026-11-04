import { RequestPriority } from './request-priority.model';
import { RequestStatus } from './request-status.model';

// Mirrors RequestDto. Named RequestItem to avoid clashing with the browser's built-in Request type.
export interface RequestItem {
  id: number;
  title: string;
  organizationName: string;
  status: RequestStatus;
  priority: RequestPriority;
  assignedTo: string | null;
  createdAt: string;
  updatedAt: string;
  rowVersion: string;
}