// Mirrors PagedResult<T>: one page of items plus paging information.
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}