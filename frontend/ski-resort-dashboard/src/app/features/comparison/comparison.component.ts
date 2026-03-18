import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Resort, SnowComparisonRow } from '../../core/models/resort.model';
import { ResortService } from '../../core/services/resort.service';

@Component({
  standalone: true,
  selector: 'app-comparison',
  imports: [
    CommonModule,
    MatCardModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatTableModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './comparison.component.html',
  styleUrl: './comparison.component.scss'
})
export class ComparisonComponent implements OnInit {
  private readonly resortService = inject(ResortService);

  allResorts: Resort[] = [];
  selectedIds: number[] = [];

  loadingResorts = true;
  loadingComparison = false;
  error?: string;

  comparisonRows: SnowComparisonRow[] = [];

  readonly displayedColumns = [
    'resort',
    'baseDepth',
    'newSnow24h',
    'newSnow72h'
  ];

  ngOnInit(): void {
    this.resortService.getResorts().subscribe({
      next: (resorts) => {
        this.allResorts = resorts;
        this.loadingResorts = false;
      },
      error: () => {
        this.error = 'Unable to load resorts.';
        this.loadingResorts = false;
      }
    });
  }

  canCompare(): boolean {
    return this.selectedIds.length > 0 && this.selectedIds.length <= 3;
  }

  compare(): void {
    if (!this.canCompare()) {
      return;
    }

    this.loadingComparison = true;
    this.error = undefined;

    this.resortService.getSnowComparison(this.selectedIds).subscribe({
      next: (rows) => {
        this.comparisonRows = rows;
        this.loadingComparison = false;
      },
      error: () => {
        this.error = 'Unable to load comparison.';
        this.loadingComparison = false;
      }
    });
  }
}

