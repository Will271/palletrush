import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { DriverSearchComponent } from './driver-search.component';
import { DriverSummary } from '../../api/drivers.service';

const ana: DriverSummary[] = [
  { id: 51, name: 'Diana Becker', teamName: 'Summit Distribution', licenseNo: 'FL-10051' },
  { id: 52, name: 'Diana Costa', teamName: 'Riverbend Storage', licenseNo: 'FL-10052' },
  { id: 101, name: 'Ivana Becker', teamName: 'Ironclad Pallets', licenseNo: 'FL-10101' },
];

const di: DriverSummary[] = [
  { id: 7, name: 'Dmitri Volkov', teamName: 'Harbor Freight Co.', licenseNo: 'FL-10007' },
];

function setup() {
  const fixture = TestBed.createComponent(DriverSearchComponent);
  const http = TestBed.inject(HttpTestingController);
  const element: HTMLElement = fixture.nativeElement;
  fixture.detectChanges();
  const input = element.querySelector<HTMLInputElement>('[data-testid="driver-search-input"]')!;
  return { fixture, http, element, input };
}

function type(input: HTMLInputElement, value: string) {
  input.value = value;
  input.dispatchEvent(new Event('input'));
}

const forSearch = (term: string) => (req: { url: string; params: { get(name: string): string | null } }) =>
  req.url === '/api/drivers' && req.params.get('search') === term;

describe('DriverSearchComponent', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({
      imports: [DriverSearchComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('typing "ana" renders results for "ana"', async () => {
    const fixture = TestBed.createComponent(DriverSearchComponent);
    const http = TestBed.inject(HttpTestingController);
    const element: HTMLElement = fixture.nativeElement;
    fixture.detectChanges();

    const input = element.querySelector<HTMLInputElement>('[data-testid="driver-search-input"]')!;
    input.value = 'ana';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    await vi.advanceTimersByTimeAsync(1000);
    fixture.detectChanges();

    http.expectOne((req) => req.urlWithParams.includes('search=ana')).flush(ana);
    await vi.advanceTimersByTimeAsync(0);
    fixture.detectChanges();

    const results = Array.from(
      element.querySelectorAll('[data-testid="driver-search-result"]'),
      (item) => item.textContent,
    );
    expect(results).toEqual(ana.map((driver) => expect.stringContaining(driver.name)));
  });

  it('does not send a request before 300ms without further input', async () => {
    const { http, input } = setup();

    type(input, 'ana');
    await vi.advanceTimersByTimeAsync(299);

    http.expectNone(forSearch('ana'));
    http.expectNone((req) => req.url === '/api/drivers');
  });

  it('sends the request once 300ms pass without further input', async () => {
    const { http, input } = setup();

    type(input, 'ana');
    await vi.advanceTimersByTimeAsync(300);

    http.expectOne(forSearch('ana')).flush(ana);
  });

  it('collapses rapid typing into a single request for the final term', async () => {
    const { http, input } = setup();

    type(input, 'a');
    await vi.advanceTimersByTimeAsync(100);
    type(input, 'an');
    await vi.advanceTimersByTimeAsync(100);
    type(input, 'ana');
    await vi.advanceTimersByTimeAsync(300);

    const requests = http.match((req) => req.url === '/api/drivers');
    expect(requests).toHaveLength(1);
    expect(requests[0].request.params.get('search')).toBe('ana');
    requests[0].flush(ana);
  });

  it('a new keystroke immediately cancels the in-flight request', async () => {
    const { fixture, http, input } = setup();

    type(input, 'ana');
    await vi.advanceTimersByTimeAsync(300);
    const staleRequest = http.expectOne(forSearch('ana'));

    type(input, 'di');
    fixture.detectChanges();
    await vi.advanceTimersByTimeAsync(0);

    expect(staleRequest.cancelled).toBe(true);

    await vi.advanceTimersByTimeAsync(299);
    http.expectNone(forSearch('di'));
    await vi.advanceTimersByTimeAsync(1);
    http.expectOne(forSearch('di')).flush(di);
  });

  it('re-entering the same query does not send a duplicate request', async () => {
    const { http, input } = setup();

    type(input, 'ana');
    await vi.advanceTimersByTimeAsync(300);
    http.expectOne(forSearch('ana')).flush(ana);

    type(input, 'ana');
    await vi.advanceTimersByTimeAsync(1000);

    http.expectNone(forSearch('ana'));
  });
});
