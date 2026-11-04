import { RequestPriority } from './request-priority.model';
import { RequestStatus } from './request-status.model';

// The values of the filters form. An empty string means "no filter".
export interface RequestFilterValues {
  search: string;
  status: RequestStatus | '';
  priority: RequestPriority | '';
  organizationName: string;
  assignedTo: string;
  createdFrom: string;
  createdTo: string;
}

export const EMPTY_FILTERS: RequestFilterValues = {
  search: '',
  status: '',
  priority: '',
  organizationName: '',
  assignedTo: '',
  createdFrom: '',
  createdTo: ''
};