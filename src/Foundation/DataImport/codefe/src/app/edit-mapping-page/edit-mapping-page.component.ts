import { Component, OnInit } from '@angular/core';
import { ItemService } from '../item.service';
import { Mapping } from './models/mapping';
import { SourceType } from './models/sourcetype';
import { Database } from './models/database';
import { Template } from './models/template';
import { Response } from './response';
import { Router, ActivatedRoute, Params } from '@angular/router';

import { SciAuthService } from '@speak/ng-sc/auth';
import { SciLogoutService } from '@speak/ng-sc/logout';
import { TreeNode } from 'angular-tree-component';
import { ITreeState } from 'angular-tree-component/dist/defs/api';
import { Source } from '../data-sources/datasources/source';
import { Target } from './models/target';

@Component({
  selector: 'app-edit-mapping-page',
  templateUrl: './edit-mapping-page.component.html',
  styleUrls: ['./edit-mapping-page.component.scss']
})
export class EditMappingPageComponent {

  objectKeys = Object.keys;

  mapping: Mapping = {
    SourceProcessingScripts: [],
    PostImportProcessingScripts:[],
    FieldMappings: [],
    Source: {} as Source,
    Target: {} as Target,
    SourceType: {} as SourceType,
    TargetType: {} as SourceType
  } as Mapping;
  
  sourceTypes: SourceType[] = [];
  messages: string[] = [];
  databases: Database[] = [];
  targetTemplates: Template[] = [];  
  isEditing = false;
  isErrorResponse = false;
  isLoading = false;
  mappingId:string = '';
  sourceType:string = '';
  sourceScriptAdditionVisible = false;
  postProcessingScriptAdditionVisible = false;
  isNavigationShown:boolean = true;
  sitecoreTree: any[] = [];
  sitecoreTreeState: ITreeState;

  sitecoreTreeOptions = {
    getChildren: (node:TreeNode) => {
      this.itemService.fetchSitecoreTree(this.mapping.Target["DatabaseName"],node.id).subscribe({
        next: data => {
          return data["data"];
        },
        error: error => {
          this.isErrorResponse = true;
          this.isLoading = false;
        }
      });
    }
  }


  constructor(
    public authService: SciAuthService,
    public logoutService: SciLogoutService,
    public itemService: ItemService,
    private route: ActivatedRoute
  ) { }

  ngOnInit() {
    // get param
    this.mappingId = this.route.snapshot.params["mappingId"];
    if(this.mappingId == "-"){
      this.mapping =  {
        SourceProcessingScripts: [],
        PostImportProcessingScripts:[],
        FieldMappings: [],
        Source: {} as Source,
        Target: {} as Target,
        SourceType: {} as SourceType,
        TargetType: {} as SourceType
      } as Mapping;
    } else{
      this.fetchMapping();    
    }
  }

  fetchMapping() {
    this.isLoading = true;
    this.isErrorResponse = false;
    if(this.mappingId != "-") {
      this.itemService.fetchMapping(this.mappingId).subscribe({
        next: data => {
          this.mapping = data["data"] as Mapping;
          this.sourceType = this.mapping.SourceType.Name;
          this.isLoading = false;
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
  
  saveMapping() {
    this.messages = [];
    this.itemService.saveMapping(this.mapping).subscribe({
      next: data => {
        this.mapping = data["data"] as Mapping;
        this.sourceType = this.mapping.SourceType.Name;
        this.isLoading = false;
        this.messages = data["messages"];          
      },
      error: error => {
        this.isErrorResponse = true;
        this.isLoading = false;
        this.messages.push("An error occured");
      }
    });
  }

  edit() {
    this.isEditing = true;
  }

  close() {
    this.isEditing = false;
  }

  isEditDisabled() {
    return this.isLoading || this.isErrorResponse || !this.mapping;
  }

  isVisible(sourceSelection,templateName){
    return sourceSelection.includes(templateName);
  }
}

