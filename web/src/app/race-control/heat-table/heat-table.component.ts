import { Component, input, output } from '@angular/core';
import { HeatSummary } from '../../api/heats.service';
import { FormatIsoPipe } from '../../shared/format-iso.pipe';

@Component({
  selector: 'app-heat-table',
  imports: [FormatIsoPipe],
  templateUrl: './heat-table.component.html',
  styleUrl: './heat-table.component.scss',
})
export class HeatTableComponent {
  readonly heats = input.required<HeatSummary[]>();
  readonly picked = output<number>();
}
