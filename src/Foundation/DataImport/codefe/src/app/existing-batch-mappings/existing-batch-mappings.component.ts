import { Component, OnInit, ViewChild } from '@angular/core';
import { SortHeaderState } from '@speak/ng-bcl/table';
import { MappingItem } from '../existing-mappings/mapping-item';
import { ActivatedRoute } from '@angular/router';
import { ItemService } from '../item.service';
import { SciLogoutService } from '@speak/ng-sc/logout';
import { SciAuthService } from '@speak/ng-sc/auth';
import { DeleteMappingDialogComponent } from '../delete-mapping-dialog/delete-mapping-dialog.component';

@Component({
  selector: 'app-existing-batch-mappings',
  templateUrl: './existing-batch-mappings.component.html',
  styleUrls: ['./existing-batch-mappings.component.scss']
})
export class ExistingBatchMappingsComponent implements OnInit {
  messages: any;
  isErrorResponse: boolean;
  isLoading: boolean;
  isNavigationShown:boolean = true;

  @ViewChild(DeleteMappingDialogComponent)
  modal: DeleteMappingDialogComponent;

  mappings: MappingItem[] = [];
  constructor(
    public authService: SciAuthService,
    public logoutService: SciLogoutService,
    public itemService: ItemService,
    private route: ActivatedRoute
  ) { }

  ngOnInit() {
    this.fetchMappings();
  }
  fetchMappings(){
    this.isLoading = true;
    this.isErrorResponse = false;
    this.itemService.fetchBatchMappings().subscribe({
      next: data => {
        this.mappings = data["data"] as MappingItem[];
        this.isLoading = false;
        this.messages = data["messages"];
      },
      error: error => {
        this.isErrorResponse = true;
        this.isLoading = false;
      }
    });  
  }
  trackByItemId(id: string, header): number { return header.id; }
  
  onSortChange(sortState: SortHeaderState[]) {
    this.mappings.sort((a, b) => {
      let result = 0;
      sortState.forEach(header => {
        if (result !== 0) {
          return;
        }
        if (a[header.id] < b[header.id]) {
          if (header.direction === 'asc') {
            result = -1;
          } else if (header.direction === 'desc') {
            result = 1;
          }
        } else if (a[header.id] > b[header.id]) {
          if (header.direction === 'asc') {
            result = 1;
          } else if (header.direction === 'desc') {
            result = -1;
          }
        }
      });
      return result;
    });
  }
  openDeleteDialog(mapping){
    this.modal.mapping = mapping;
    this.modal.openModal();
  }
}
