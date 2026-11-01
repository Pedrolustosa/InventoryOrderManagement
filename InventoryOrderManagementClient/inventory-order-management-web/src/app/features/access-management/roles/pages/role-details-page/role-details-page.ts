import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Role } from '../../models/role.model';
import { RolesService } from '../../services/roles.service';

@Component({
  selector: 'app-role-details-page',
  imports: [RouterLink],
  templateUrl: './role-details-page.html',
})
export class RoleDetailsPage implements OnInit {
  private readonly rolesService = inject(RolesService);
  private readonly route = inject(ActivatedRoute);

  readonly role = signal<Role | null>(null);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      return;
    }

    this.rolesService.getRole(id).subscribe({
      next: (role) => this.role.set(role),
      error: (error: Error) => this.errorMessage.set(error.message),
    });
  }
}
