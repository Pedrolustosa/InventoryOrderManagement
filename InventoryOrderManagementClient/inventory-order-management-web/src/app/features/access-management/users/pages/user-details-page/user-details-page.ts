import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { UserAccount } from '../../models/user.model';
import { UsersService } from '../../services/users.service';

@Component({
  selector: 'app-user-details-page',
  imports: [DatePipe, RouterLink],
  templateUrl: './user-details-page.html',
})
export class UserDetailsPage implements OnInit {
  private readonly usersService = inject(UsersService);
  private readonly route = inject(ActivatedRoute);

  readonly user = signal<UserAccount | null>(null);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      return;
    }

    this.usersService.getUser(id).subscribe({
      next: (user) => this.user.set(user),
      error: (error: Error) => this.errorMessage.set(error.message),
    });
  }
}
