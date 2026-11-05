import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { BulkUpdateItem, BulkUpdateResult } from '../models/bulk-update.model';
import { PagedResult } from '../models/paged-result.model';
import { RequestHistoryEntry } from '../models/request-history-entry.model';
import { RequestItem } from '../models/request-item.model';
import { RequestQuery } from '../models/request-query.model';
import { RequestStatistics } from '../models/request-statistics.model';
import { RequestStatus } from '../models/request-status.model';

/**
 * The only place that talks to the requests API.
 * Components never use HttpClient directly.
 */
@Injectable({ providedIn: 'root' })
export class RequestsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/requests';

  // Filtering, sorting and paging are sent as query parameters and run on the server.
  getRequests(query: RequestQuery): Observable<PagedResult<RequestItem>> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize)
      .set('sortBy', query.sortBy)
      .set('sortDirection', query.sortDirection);

    // Optional filters are sent only when they have a value.
    const optional: Record<string, string | null> = {
      status: query.status,
      priority: query.priority,
      organizationName: query.organizationName.trim(),
      assignedTo: query.assignedTo.trim(),
      createdFrom: query.createdFrom,
      createdTo: query.createdTo,
      search: query.search.trim()
    };

    for (const [key, value] of Object.entries(optional)) {
      if (value) {
        params = params.set(key, value);
      }
    }

    return this.http.get<PagedResult<RequestItem>>(this.baseUrl, { params });
  }

  getById(id: number): Observable<RequestItem> {
    return this.http.get<RequestItem>(`${this.baseUrl}/${id}`);
  }

  getStatistics(): Observable<RequestStatistics> {
    return this.http.get<RequestStatistics>(`${this.baseUrl}/statistics`);
  }

  getHistory(id: number): Observable<RequestHistoryEntry[]> {
    return this.http.get<RequestHistoryEntry[]>(`${this.baseUrl}/${id}/history`);
  }

  // The RowVersion the user saw is sent back; the server returns 409 if the request changed since then.
  updateStatus(id: number, newStatus: RequestStatus, rowVersion: string, userName: string): Observable<RequestItem> {
    return this.http.patch<RequestItem>(
      `${this.baseUrl}/${id}/status`,
      { newStatus, rowVersion },
      { headers: { 'X-User-Name': userName } }
    );
  }

  // Partial success: the response reports the outcome of every item.
  bulkUpdateStatus(newStatus: RequestStatus, items: BulkUpdateItem[], userName: string): Observable<BulkUpdateResult> {
    return this.http.patch<BulkUpdateResult>(
      `${this.baseUrl}/status`,
      { newStatus, items },
      { headers: { 'X-User-Name': userName } }
    );
  }
}