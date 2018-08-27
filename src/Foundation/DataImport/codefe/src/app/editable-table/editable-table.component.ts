import { Component, OnInit, Input, ViewChild, TemplateRef, AfterViewInit } from '@angular/core';
import { ScTable } from '@speak/ng-bcl/table';
import { ScDialog, ScDialogWindow, ScDialogService, ScDialogBackdrop, ScDialogModule } from '@speak/ng-bcl/dialog';
import { Mapping } from '../edit-mapping-page/models/mapping';
import { SciAuthService } from '@speak/ng-sc/auth';
import { SciLogoutService } from '@speak/ng-sc/logout';
import { ItemService } from '../item.service';
import { ActivatedRoute } from '@angular/router';
import { Template } from '../edit-mapping-page/models/template';
import { FieldMapping } from '../edit-mapping-page/models/FieldMapping';
import { retry } from 'rxjs/operators/retry';
import { Field } from '../edit-mapping-page/models/field';
import { Dictionary } from '@speak/ng-bcl';
import { DictionaryObject } from '../edit-mapping-page/models/dictionary';
import { ScriptEditingDialogComponent } from '../script-editing-dialog/script-editing-dialog.component';

@Component({
  selector: '[scEditableTable]',
  templateUrl: './editable-table.component.html',
  styleUrls: ['./editable-table.component.scss']
})

export class ScEditableTable implements OnInit {
  ngOnInit(): void {
    
  }
  @Input() targetTemplates: Template[];
  messages: any;
  targetFields: Field[];
  isErrorResponse: boolean;
  isLoading: boolean;
  fields:DictionaryObject[] = [];
  addFieldMappingVisible: boolean = true;

  @Input() mapping: Mapping;

  @ViewChild(ScriptEditingDialogComponent)
  modal: ScriptEditingDialogComponent;
  
    constructor(public authService: SciAuthService,
      public logoutService: SciLogoutService,
      public itemService: ItemService,
      private route: ActivatedRoute
    ) { 

    }
    ngOnChanges() {
      this.fetchFields();
      this.fetchTemplates();
    }

    fetchTemplates(){
      if(this.mapping && this.mapping.Target && this.mapping.Target["DatabaseName"]){
        this.isLoading = true;
        this.isErrorResponse = false;
        this.itemService.fetchTemplates(this.mapping.Target["DatabaseName"]).subscribe({
          next: data => {
            this.targetTemplates = data["data"] as Template[];
            this.isLoading = false;
            this.messages = data["messages"];          
          },
          error: error => {
            this.targetTemplates = [];
            this.isErrorResponse = true;
            this.isLoading = false;
          }
        });
      }
    }
    fetchFields(){
      if(this.mapping && this.mapping.Target && this.mapping.Target["DatabaseName"]){
        this.isLoading = true;
        this.isErrorResponse = false;
        this.itemService.fetchFields(this.mapping.Target["DatabaseName"],this.mapping.Target["TemplateId"]).subscribe({
          next: data => {
            this.targetFields = data["data"] as Field[];
            this.isLoading = false;
            this.messages = data["messages"];          
          },
          error: error => {
            this.targetFields = [];
            this.isErrorResponse = true;
            this.isLoading = false;
          }
        });
        if(this.mapping.FieldMappings != null){
          this.mapping.FieldMappings.forEach(fieldMapping => {
            if(fieldMapping.ReferenceItemsTemplate){
              this.loadReferenceTemplateFields(fieldMapping.ReferenceItemsTemplate);
            }
          });
        }
      }
    }
    isFieldScriptsChecked(field:FieldMapping){
      if(field && field.ProcessingScripts && field.ProcessingScripts.length > 0){
        return true;
      }
      return false;
    }
    loadReferenceTemplateFields(targetTemplateId){
      this.itemService.fetchFields(this.mapping.Target["DatabaseName"],targetTemplateId).subscribe({
        next: data => {
          var dicEntry = { Key:targetTemplateId, Value: data["data"] as DictionaryObject};          
          this.fields.push(dicEntry);
          this.isLoading = false;
          this.messages = data["messages"];      
        },
        error: error => {
          this.isErrorResponse = true;
          this.isLoading = false;
        }
      });
    }
    onTemplateChange(target){
      var targetTemplateId = target.value;
      this.itemService.fetchFields(this.mapping.Target["DatabaseName"],targetTemplateId).subscribe({
        next: data => {
          var dicEntry = { Key:targetTemplateId, Value: data["data"] as DictionaryObject};          
          this.fields.push(dicEntry);
          this.isLoading = false;
          this.messages = data["messages"];      
        },
        error: error => {
          this.isErrorResponse = true;
          this.isLoading = false;
        }
      });
    }
    getFields(templateId){
      var dicEntry = this.fields.find(i=>i.Key == templateId);
      if(dicEntry) {
        return dicEntry.Value
      } 
      return [];
    }
    openScriptList(modalId, field){  
      this.modal.mapping = field;
      this.modal.openModal(modalId);      
    }
    showAddFieldMapping(){
      this.addFieldMappingVisible = true;
      if(!this.mapping.FieldMappings) {
        this.mapping.FieldMappings = [] as FieldMapping[];
      }
      this.mapping.FieldMappings.push(
        {
          ProcessingScripts: [] as string[]
        } as FieldMapping
      );
    }
    isSitecoreDatasource(){
      return this.mapping.SourceType && this.mapping.SourceType.Name && this.mapping.SourceType.Name.indexOf('Sitecore')>-1;
    }
    deleteMapping(field){
      var index = this.mapping.FieldMappings.findIndex(f=> f == field);
      if (index > -1) {
        this.mapping.FieldMappings.splice(index, 1);
      }
    }
  }
