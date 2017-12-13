import { Component, OnInit, Input, ViewChild, ChangeDetectorRef } from '@angular/core';
import { Mapping } from '../edit-mapping-page/models/mapping';
import { SciAuthService } from '@speak/ng-sc/auth';
import { SciLogoutService } from '@speak/ng-sc/logout';
import { ItemService } from '../item.service';
import { ActivatedRoute } from '@angular/router';
import { Database } from '../edit-mapping-page/models/database';
import { Template } from '../edit-mapping-page/models/template';
import { TreeNode, TreeComponent, ITreeState, ITreeOptions } from 'angular-tree-component';
import { find } from 'rxjs/operator/find';
import { ITreeModel, ITreeNode } from 'angular-tree-component/dist/defs/api';
import { forEach } from '@angular/router/src/utils/collection';
import { SourceType } from '../edit-mapping-page/models/sourcetype';
import { Target } from '../edit-mapping-page/models/target';

@Component({
  selector: 'xc-target-system',
  templateUrl: './target-system.component.html',
  styleUrls: ['./target-system.component.scss']
})
export class TargetSystemComponent implements OnInit {
  targetType: string = '';
  targetOptionsDic: any[] = [];
  targetTypes: any[];
  targetTemplates: any[];
  databases: any[];
  sitecoreTreeState: ITreeState;
  messages: any;
  sitecoreTree: TreeNode[];
  isErrorResponse: boolean;
  isLoading: boolean;
  sitecoreTreeOptions: ITreeOptions = {};
  isInitialLoading:boolean = true;
  activeId:string;

  @Input() mapping: Mapping = {} as Mapping;

  @ViewChild(TreeComponent)
  private tree: TreeComponent;

  constructor(public authService: SciAuthService,
    public logoutService: SciLogoutService,
    public itemService: ItemService,
    private route: ActivatedRoute,
    private cd: ChangeDetectorRef) 
  {

  }

  ngOnInit() {    
    this.fetchTargetTypes();    
  }
  ngOnChanges(){
    this.sitecoreTreeOptions = {
      getChildren: (node:TreeNode) => {
        let promise = new Promise((resolve, reject) => {
          this.itemService.fetchChildNodes(this.mapping.Target["DatabaseName"],node.id)
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

  fetchSitecoreTree(optionsSourceUrl:string, key:string) {
    if(!key){
      key = optionsSourceUrl;
    }
    if(this.mapping && this.mapping.Target && this.mapping.Target["FullPath"])
    {
        var expandedIds = this.mapping.Target["FullPath"].trim().split("/");
        this.activeId = expandedIds.pop();
    }
    if(this.mapping.Target["DatabaseName"]){
      this.isLoading = true;
      this.isErrorResponse = false;
      this.itemService.fetchSitecoreTreeOptions(optionsSourceUrl,this.mapping.Target["FullPath"]).subscribe({
        next: data => {
          this.targetOptionsDic[key] = data["data"] as TreeNode[];
          this.targetOptionsDic[key] = [].concat(this.targetOptionsDic[key]);
          this.isLoading = false;
          this.messages = data["messages"];           
          this.cd.detectChanges();            
          //.log(key + " " + this.targetOptionsDic[key]); 
        },
        error: error => {
          this.isErrorResponse = true;
          this.isLoading = false;
        }
      });
    }
  }

  onActivate(evt){
    if(evt.node){
      this.mapping.Target["ItemPath"] = evt.node.data.path;
      this.mapping.Target["FullPath"] = evt.node.data.longId;
      this.activeId = evt.node.data.id;
    }
    //console.log(this.tree.treeModel.getActiveNodes());
  }
  setState(e){
    if(this.activeId){
      var node = this.tree.treeModel.getNodeById(this.activeId) as ITreeNode;
      if(node && !node.isActive){
        this.tree.treeModel.setActiveNode(node,true);
      }
    }
  }
  fetchTargetTypes(){
    this.isLoading = true;
    this.isErrorResponse = false;
    this.itemService.fetchTargetTypes().subscribe({
      next: data => {
        this.targetTypes = data["data"] as SourceType[];
        this.isLoading = false;
        this.messages = data["messages"];
        if(this.targetTypes){
          this.targetTypes.forEach((src) => {
            if(src.Fields){
              src.Fields.forEach((fld) => {
                if(fld.OptionsSource){
                  this.fetchDependentOptions(fld);
                  if(fld.TriggerField){
                    this.loadDependentFields(fld.TriggerField, src);
                  }
                }
              });
            }
          });
        }
      },
      error: error => {
        this.targetTypes = [];
        this.isErrorResponse = true;
        this.isLoading = false;
      }
    });
  }

  getOptions(optionsSourceUrl:string, key:string="") {
    //console.log(optionsSourceUrl);
    if(!key){
      key = optionsSourceUrl;
    }
    if(optionsSourceUrl.indexOf("[") === -1){
      this.isLoading = true;
      this.isErrorResponse = false;
        this.itemService.fetchSourceOptions(optionsSourceUrl).subscribe({ 
          next: data => {          
            this.targetOptionsDic[key] = (data["data"] as any[]);
            this.isLoading = false;
            this.messages = data["messages"];
            this.cd.detectChanges();  
            //console.log("target: " + key + ":" + this.targetOptionsDic[key]);
          },
          error: error => {
            this.targetOptionsDic[key] = [];
            this.isErrorResponse = true;
            this.isLoading = false;
          }
        });
    }
   return this.targetOptionsDic[key];
  }

  processOptionsUrl(url:string){
    var regex = new RegExp("\\[(.*)\\]");
    if(regex.test(url)){
      var match = regex.exec(url);
      if(match[0] && match[1] && this.mapping.Target[match[1]]){
        return url.replace(match[0],this.mapping.Target[match[1]]);
      }
    }
    return url;
  }

  triggerFields(fieldNames:string){
    if(this.targetTypes && fieldNames){
      var fields = fieldNames.split(",");
      this.targetTypes.forEach((src) => {
        if(src.Fields){
          src.Fields.forEach((fld) => {
            if(fields.find(f=> f === fld.Name)) {
              this.fetchDependentOptions(fld);
            }
          });
        }
      });
    }
  }

  loadDependentFields(fields: string, source:SourceType) {
    var dependentFieldNames = fields.trim().split(",");
    dependentFieldNames.forEach((field)=> {
      var fld = source.Fields.find(f=>f.Name === field);
      if(fld){
        this.fetchDependentOptions(fld);
      }
    });    
  }
  fetchDependentOptions(fld){
    var sourceUrl = this.processOptionsUrl(fld.OptionsSource);
    if(fld.InputType === 'tree'){
      this.fetchSitecoreTree(sourceUrl, fld.OptionsSource);
    } else{
      this.getOptions(sourceUrl, fld.OptionsSource);              
    }
  }
}
