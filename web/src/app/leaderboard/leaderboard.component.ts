import { Component, inject, input, numberAttribute, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { catchError, EMPTY, of, switchMap, timer } from 'rxjs';
import { HeatsService, PenaltyEntry } from '../api/heats.service';
import { FormatIsoPipe } from '../shared/format-iso.pipe';
import { RunTimePipe } from '../shared/run-time.pipe';

@Component({
  selector: 'app-leaderboard',
  imports: [RunTimePipe, FormatIsoPipe],
  templateUrl: './leaderboard.component.html',
  styleUrl: './leaderboard.component.scss',
})
export class LeaderboardComponent {
  private readonly heatsService = inject(HeatsService);

  readonly heatId = input.required({ transform: numberAttribute });
  readonly selectedRunId = signal<string | null>(null);

  readonly entries = toSignal(
    toObservable(this.heatId).pipe(
      switchMap((heatId) =>
        timer(0, 3000).pipe(
          switchMap(() => this.heatsService.getLeaderboard(heatId).pipe(catchError(() => EMPTY))),
        ),
      ),
    ),
    { initialValue: [] },
  );

  readonly penalties = toSignal(
    toObservable(this.selectedRunId).pipe(
      switchMap((runId) =>
        runId ? this.heatsService.getPenalties(runId).pipe(catchError(() => of<PenaltyEntry[]>([]))) : of<PenaltyEntry[]>([]),
      ),
    ),
    { initialValue: [] },
  );

  selectRun(runId: string) {
    this.selectedRunId.set(runId);
  }
}
