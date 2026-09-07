import { Component, ElementRef, ViewChild, inject, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SessionService } from './core/api.service';
import { Icon } from './shared/icon';
@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, FormsModule, Icon],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  session = inject(SessionService);
  sidebar = signal(false);
  profile = signal(false);
  notifications = signal(false);
  token = '';
  @ViewChild('connection') connection!: ElementRef<HTMLDialogElement>;
  nav = [
    { label: 'Dashboard', icon: 'dashboard' },

    { label: 'Leave', icon: 'calendar' },
    { label: 'Payroll', icon: 'wallet' },
    { label: 'Payslips', icon: 'file' },
    { label: 'Reports', icon: 'chart' },
  ];
  openConnection() {
    this.profile.set(false);
    this.token = '';
    this.connection.nativeElement.showModal();
  }
  connect() {
    this.session.token.set(this.token.trim());
    this.token = '';
    this.connection.nativeElement.close();
  }
  disconnect() {
    this.session.token.set('');
    this.profile.set(false);
  }
}
