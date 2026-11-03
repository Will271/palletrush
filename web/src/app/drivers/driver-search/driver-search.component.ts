import { Component, DestroyRef, inject, signal } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { distinctUntilChanged, switchMap, timer } from 'rxjs';
import { DriversService, DriverSummary } from '../../api/drivers.service';

@Component({
  selector: 'app-driver-search',
  templateUrl: './driver-search.component.html',
  styleUrl: './driver-search.component.scss',
})
export class DriverSearchComponent {
  private readonly driversService = inject(DriversService);
  private readonly destroyRef = inject(DestroyRef);

  readonly query = signal('');
  readonly results = signal<DriverSummary[]>([]);

  constructor() {
    const subscription = toObservable(this.query)
      .pipe(
        distinctUntilChanged(),
        switchMap((search) =>
          timer(300).pipe(
            switchMap(() => this.driversService.search(search)),
          ),
        ),
      )
      .subscribe((r) => this.results.set(r));

    this.destroyRef.onDestroy(() => subscription.unsubscribe());
  }
}
