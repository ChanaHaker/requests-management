import { Component, inject } from '@angular/core';
import { RequestsStateService } from '../../services/requests-state.service';
import { RequestDetails } from '../request-details/request-details';
import { RequestFilters } from '../request-filters/request-filters';
import { RequestStatistics } from '../request-statistics/request-statistics';
import { RequestsTable } from '../requests-table/requests-table';

/**
 * The requests screen. Only displays the state and forwards user actions to the state service.
 * No HTTP calls and no RxJS here.
 */
@Component({
  selector: 'app-requests-page',
  imports: [RequestDetails, RequestFilters, RequestStatistics, RequestsTable],
  templateUrl: './requests-page.html',
  styleUrl: './requests-page.css'
})
export class RequestsPage {
  readonly state = inject(RequestsStateService);
}