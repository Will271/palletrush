import { Component, input, output } from '@angular/core';
import { RegistrationEntry } from '../../api/heats.service';
import { FormatIsoPipe } from '../../shared/format-iso.pipe';

@Component({
  selector: 'app-registration-table',
  imports: [FormatIsoPipe],
  templateUrl: './registration-table.component.html',
  styleUrl: './registration-table.component.scss',
})
export class RegistrationTableComponent {
  readonly registrations = input.required<RegistrationEntry[]>();
  readonly simulate = output<RegistrationEntry>();
}
