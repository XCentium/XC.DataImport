import { Component, OnInit, Input, ViewChild, ChangeDetectorRef } from '@angular/core';
import { SitecoreDataSource } from './datasources/sitecoredatasource';
import { SitecoreQueryDataSource } from './datasources/sitecorequerydatasource';
import { SqlDataSource } from './datasources/sqldatasource';
import { FolderDataSource } from './datasources/folderdatasource';
import { WebDataSource } from './datasources/webdatasource';
import { FileDataSource } from './datasources/filedatasource';
import { SciAuthService } from '@speak/ng-sc/auth';
import { SciLogoutService } from '@speak/ng-sc/logout';
import { ItemService } from '../item.service';
import { ActivatedRoute } from '@angular/router';
import { SourceType } from '../edit-mapping-page/models/sourcetype';
import { Mapping } from '../edit-mapping-page/models/mapping';
import { Ng4FilesSelected, Ng4FilesStatus, Ng4FilesService, Ng4FilesConfig } from 'angular4-files-upload';
import { Template } from '../edit-mapping-page/models/template';
import { Database } from '../edit-mapping-page/models/database';
import { TreeNode, ITreeOptions, ITreeState, TreeComponent } from 'angular-tree-component';
import { ITreeNode } from 'angular-tree-component/dist/defs/api';
import { retry } from 'rxjs/operators/retry';
import { Observable } from 'rxjs/Observable';
import { InputField } from '../edit-mapping-page/models/inputfield';

@Component({
  selector: 'xc-datasources',
  templateUrl: './data-sources.component.html',
  styleUrls: ['./data-sources.component.scss']
})
export class DataSourcesComponent implements OnInit {
  
  activeId: any;
  databases: any[];
  sourceTemplates: any[];
  @Input() mapping: Mapping = {} as Mapping;

  messages: any;
  sourceTypes: SourceType[];
  isErrorResponse: boolean;
  isLoading: boolean;
  mappingId:string = '';
  sourceType:string = '';
  fileInputVisible:boolean = true;
  optionsDic: any[];
  sitecoreTree: TreeNode[];
  sitecoreTreeOptions: ITreeOptions = {};
  sitecoreTreeState: ITreeState;

  @ViewChild(TreeComponent)
  private tree: TreeComponent;

  constructor(
    public authService: SciAuthService,
    public logoutService: SciLogoutService,
    public itemService: ItemService,
    private route: ActivatedRoute,
    private cd: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.fetchSourceTypes();
    if(this.mapping.Source  as FileDataSource) {
      if(this.mapping.Source as FileDataSource && !(this.mapping.Source as FileDataSource).FilePath){
        this.fileInputVisible = false;
      } 
    } 
    this.optionsDic = [];

    this.sitecoreTreeOptions = {
      getChildren: (node:TreeNode) => {
        let promise = new Promise((resolve, reject) => {
          this.itemService.fetchChildNodes(this.mapping.Source["DatabaseName"],node.id)
          .toPromise()
          .then(
            data => {
              resolve(data as TreeNode[]);
            });
        });
        return promise;
      },
      nodeHeight: -2
    }    
  }

  fetchSourceTypes(){
    this.isLoading = true;
    this.isErrorResponse = false;
    this.itemService.fetchSourceTypes().subscribe({
      next: data => {
        this.sourceTypes = data["data"] as SourceType[];
        this.isLoading = false;
        this.messages = data["messages"];
        if(this.sourceTypes){
          this.sourceTypes.forEach((src) => {
            if(src.Fields){
              src.Fields.forEach((fld) => {
                if(fld.OptionsSource){
                  var sourceUrl = this.processOptionsUrl(fld.OptionsSource);
                  this.getOptions(sourceUrl);
                }
              });
            }
          });
        }
      },
      error: error => {
        this.sourceTypes = [];
        this.isErrorResponse = true;
        this.isLoading = false;
      }
    });
  }
  
  getOptions(optionsSourceUrl:string, key:string="") {
    console.log(optionsSourceUrl);
    if(!key){
      key = optionsSourceUrl;
    }
    if(optionsSourceUrl.indexOf("[") === -1){
      this.isLoading = true;
      this.isErrorResponse = false;
        this.itemService.fetchSourceOptions(optionsSourceUrl).subscribe({ 
          next: data => {          
            this.optionsDic[key] = (data["data"] as any[]);
            this.isLoading = false;
            this.messages = data["messages"];
            console.log(this.optionsDic[key]);
          },
          error: error => {
            this.optionsDic[key] = [];
            this.isErrorResponse = true;
            this.isLoading = false;
          }
        });
    }
   return this.optionsDic[key];
  }

  processOptionsUrl(url:string){
    var regex = new RegExp("\\[(.*)\\]");
    if(regex.test(url)){
      var match = regex.exec(url);
      if(match[0] && match[1] && this.mapping.Source[match[1]]){
        return url.replace(match[0],this.mapping.Source[match[1]]);
      }
    }
    return url;
  }

  showSource(sourceSelection){
    this.sourceType = sourceSelection.selectedOptions[0].label;
  }

  showFileInput(){
    this.fileInputVisible = true;
  }

  triggerFields(fieldNames:string){
    if(this.sourceTypes && fieldNames){
      var fields = fieldNames.split(",");
      this.sourceTypes.forEach((src) => {
        if(src.Fields){
          src.Fields.forEach((fld) => {
            if(fields.find(f=> f === fld.Name)) {
              var sourceUrl = this.processOptionsUrl(fld.OptionsSource);
              if(fld.InputType === 'tree'){
                this.fetchSitecoreTree(sourceUrl, fld.OptionsSource);
              } else{
                this.getOptions(sourceUrl, fld.OptionsSource);              
              }
            }
          });
        }
      });
    }
  }

  onActivate(evt){
    if(evt.node){
      this.mapping.Source["ItemPath"] = evt.node.data.path;
      this.mapping.Source["FullPath"] = evt.node.data.longId;
      this.activeId = evt.node.data.id;
    }
    console.log(this.tree.treeModel.getActiveNodes());
  }
  setState(e){
    if(this.activeId){
      var node = this.tree.treeModel.getNodeById(this.activeId) as ITreeNode;
      if(node && !node.isActive){
        this.tree.treeModel.setActiveNode(node,true);
      }
    }
  }
  fetchSitecoreTree(optionsSourceUrl:string, key:string) {
    if(!key){
      key = optionsSourceUrl;
    }
    if(this.mapping && this.mapping.Source && this.mapping.Source["FullPath"])
    {
        var expandedIds = this.mapping.Source["FullPath"].trim().split("/");
        this.activeId = expandedIds.pop();
    }
    if(this.mapping.Source["DatabaseName"]){
      this.isLoading = true;
      this.isErrorResponse = false;
      this.itemService.fetchSitecoreTreeOptions(optionsSourceUrl,this.mapping.Source["FullPath"]).subscribe({
        next: data => {
          this.optionsDic[key] = data["data"] as TreeNode[];
          this.sitecoreTree = data["data"] as TreeNode[];
          this.isLoading = false;
          this.messages = data["messages"]; 
          this.cd.detectChanges();  
          console.log(key + " " + this.optionsDic[key]); 
        },
        error: error => {
          this.isErrorResponse = true;
          this.isLoading = false;
        }
      });
    }
  }
}
