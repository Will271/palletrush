import { Component, DestroyRef, inject, signal } from '@angular/core';
import { DriversService, OnCourseDriver } from '../../api/drivers.service';

@Component({
  selector: 'app-on-course',
  templateUrl: './on-course.component.html',
  styleUrl: './on-course.component.scss',
})
export class OnCourseComponent {
  private readonly driversService = inject(DriversService);
  private readonly destroyRef = inject(DestroyRef);

  readonly drivers = signal<OnCourseDriver[]>([]);

  constructor() {
    const id = setInterval(
      () => this.driversService.getOnCourse().subscribe((r) => this.drivers.set(r)),
      2000,
    );
    this.destroyRef.onDestroy(() => clearInterval(id));
  }
}
