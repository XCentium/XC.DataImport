import { Component, OnInit, ViewChild, TemplateRef, ViewChildren, QueryList, Input, Output } from '@angular/core';
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
  @Output() messages: string[] = [];
  isErrorResponse: boolean;
  isLoading: boolean;
  @Input() mapping: MappingItem = {} as MappingItem;
  @Output() deleted:boolean = false;

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
    this.deleteMapping();
  }

  deleteMapping() {
    this.isLoading = true;
    this.isErrorResponse = false;
    this.itemService.deleteMapping(this.mapping.Id).subscribe({
      next: data => {
        this.messages = data["messages"] as string[];
        this.deleted = true;
        this.modalService.close(this.messages);    
        window.location.reload();    
      },
      error: error => {
        this.isErrorResponse = true;
        this.isLoading = false;
        this.messages.push("Mapping wasn't found");
      }
    });       
  }
}
