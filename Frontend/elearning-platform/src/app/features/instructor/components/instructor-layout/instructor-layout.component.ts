import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { HeaderComponent } from '@shared/components/header/header.component';
import { FooterComponent } from '@shared/components/footer/footer.component';

@Component({
  selector: 'app-instructor-layout',
  standalone: true,
  imports: [RouterModule, HeaderComponent, FooterComponent],
  templateUrl: './instructor-layout.component.html'
})
export class InstructorLayoutComponent {}
