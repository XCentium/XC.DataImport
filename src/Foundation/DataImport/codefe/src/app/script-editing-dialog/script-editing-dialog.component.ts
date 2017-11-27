import { Component, OnInit, Input, ViewChild, TemplateRef, ViewChildren, QueryList, AfterViewInit } from '@angular/core';
import { ScDialogService } from '@speak/ng-bcl/dialog';
import { FieldMapping } from '../edit-mapping-page/models/FieldMapping';
import { ModalDirective } from './modal.directive';

@Component({
  selector: 'xc-script-editing-dialog',
  templateUrl: './script-editing-dialog.component.html',
  styleUrls: ['./script-editing-dialog.component.scss']
})
export class ScriptEditingDialogComponent implements AfterViewInit {  
  @Input() mapping: FieldMapping = {} as FieldMapping;

  scripts: string[] = [];
  
  @ViewChild('modalTemplate')
  templateRef: TemplateRef<any>;

  @ViewChildren(ModalDirective)
  private queryList: QueryList<ModalDirective> 

  constructor(private modalService: ScDialogService) {}

  ngAfterViewInit() {
    
  }	

  ngOnInit() {

  }

  openModal(id){
    this.scripts = this.mapping.ProcessingScripts;
    this.modalService.open(this.templateRef);
  }

  closeModal(){
      this.modalService.close();
  }
  onClick(target){
    this.mapping.ProcessingScripts =  this.scripts;
    this.modalService.close(this.mapping);
  }
}
