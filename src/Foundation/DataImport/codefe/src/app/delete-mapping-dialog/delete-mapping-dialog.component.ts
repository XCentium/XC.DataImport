import { Component, OnInit, ViewChild, TemplateRef, ViewChildren, QueryList, Input } from '@angular/core';
import { ScDialogService } from '@speak/ng-bcl/dialog';
import { ItemService } from '../item.service';
import { MappingItem } from '../existing-mappings/mapping-item';
import { DeleteModalDirective } from './modal.directive';

@Component({
  selector: 'xc-delete-mapping-dialog',
  templateUrl: './delete-mapping-dialog.component.html',
  styleUrls: ['./delete-mapping-dialog.component.scss']
})
export class DeleteMappingDialogComponent implements OnInit {
  messages: string[];
  isErrorResponse: boolean;
  isLoading: boolean;
  @Input() mapping: MappingItem = {} as MappingItem;

  @ViewChild('modalTemplate')
  templateRef: TemplateRef<any>;

  @ViewChildren(DeleteModalDirective)
  private queryList: QueryList<DeleteModalDirective> 

  constructor(private modalService: ScDialogService,
    public itemService: ItemService) {}

  ngOnInit() {
  }
  openModal(){
    this.modalService.open(this.templateRef);
  }

  closeModal(){
      this.modalService.close();
  }
  onClick(target){
    this.modalService.close(this.mapping.Id);
  }

  deleteMapping() {
    this.isLoading = true;
    this.isErrorResponse = false;
    this.itemService.deleteMapping(this.mapping.Id).subscribe({
      next: data => {
        this.messages = data["messages"];
      },
      error: error => {
        this.isErrorResponse = true;
        this.isLoading = false;
        this.messages.push("Mapping wasn't found");
      }
    });       
  }
}
