import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'formatIso' })
export class FormatIsoPipe implements PipeTransform {
  transform(value: string): string {
    const m = value.match(/^(\d{4}-\d{2}-\d{2})T(\d{2}:\d{2}:\d{2})(\.\d{1,4})?/);
    return m ? `${m[1]} ${m[2]}${m[3] ?? ''}` : value;
  }
}
