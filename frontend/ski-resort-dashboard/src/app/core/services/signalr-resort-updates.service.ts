import { Injectable } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel
} from '@microsoft/signalr';
import { Subject } from 'rxjs';

export interface ResortConditionsUpdatedEvent {
  resortId: number;
  payload: unknown;
}

@Injectable({
  providedIn: 'root'
})
export class SignalrResortUpdatesService {
  private hub?: HubConnection;
  private readonly updatesSubject = new Subject<ResortConditionsUpdatedEvent>();

  readonly updates$ = this.updatesSubject.asObservable();

  start(): void {
    if (this.hub && this.hub.state !== HubConnectionState.Disconnected) {
      return;
    }

    this.hub = new HubConnectionBuilder()
      .withUrl('/hubs/resort-conditions')
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    this.hub.on('ResortConditionsUpdated', (resortId: number, payload: unknown) => {
      this.updatesSubject.next({ resortId, payload });
    });

    void this.hub
      .start()
      .catch((err) => console.error('SignalR connection failed', err));
  }

  subscribeToResort(resortId: number): void {
    if (!this.hub || this.hub.state !== HubConnectionState.Connected) {
      return;
    }

    void this.hub.invoke('SubscribeToResort', resortId).catch((err) => {
      console.error('Failed to subscribe to resort updates', err);
    });
  }

  unsubscribeFromResort(resortId: number): void {
    if (!this.hub || this.hub.state !== HubConnectionState.Connected) {
      return;
    }

    void this.hub.invoke('UnsubscribeFromResort', resortId).catch((err) => {
      console.error('Failed to unsubscribe from resort updates', err);
    });
  }

  stop(): void {
    if (!this.hub) {
      return;
    }

    void this.hub.stop().catch((err) => {
      console.error('Failed to stop SignalR connection', err);
    });
  }
}

