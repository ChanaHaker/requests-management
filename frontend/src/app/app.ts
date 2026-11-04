import { Component } from '@angular/core';
import { RequestsPage } from './components/requests-page/requests-page';

@Component({
  selector: 'app-root',
  imports: [RequestsPage],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {}