import { Component, OnInit, ViewEncapsulation, ViewChild } from '@angular/core';
import { MappingItem } from './mapping-item';
import { SciAuthService } from '@speak/ng-sc/auth';
import { SciLogoutService } from '@speak/ng-sc/logout';
import { ItemService } from '../item.service';
import { ActivatedRoute } from '@angular/router';
import { SortHeaderState } from '@speak/ng-bcl/table';
import { DeleteMappingDialogComponent } from '../delete-mapping-dialog/delete-mapping-dialog.component';

@Component({
  selector: 'app-existing-mappings',
  templateUrl: './existing-mappings.component.html',
  styleUrls: ['./existing-mappings.component.scss'],
  encapsulation: ViewEncapsulation.None
})
export class ExistingMappingsComponent implements OnInit {
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
    this.itemService.fetchMappings().subscribe({
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
