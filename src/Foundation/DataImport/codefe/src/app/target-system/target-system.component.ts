import { Component, OnInit, Input, ViewChild, ChangeDetectorRef } from '@angular/core';
import { Mapping } from '../edit-mapping-page/models/mapping';
import { SciAuthService } from '@speak/ng-sc/auth';
import { SciLogoutService } from '@speak/ng-sc/logout';
import { ItemService } from '../item.service';
import { ActivatedRoute } from '@angular/router';
import { Database } from '../edit-mapping-page/models/database';
import { Template } from '../edit-mapping-page/models/template';
import { LoadService } from '../edit-mapping-page/load.service';
import { TreeNode, TreeComponent, ITreeState, ITreeOptions } from 'angular-tree-component';
import { find } from 'rxjs/operator/find';
import { ITreeModel, ITreeNode } from 'angular-tree-component/dist/defs/api';
import { forEach } from '@angular/router/src/utils/collection';

@Component({
  selector: 'xc-target-system',
  templateUrl: './target-system.component.html',
  styleUrls: ['./target-system.component.scss']
})
export class TargetSystemComponent implements OnInit {
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
    private loadService: LoadService,
    private cd: ChangeDetectorRef) 
  {
    
  }

  ngOnInit() {
    this.fetchDatabases();    
    
    this.sitecoreTreeOptions = {
      getChildren: (node:TreeNode) => {
        let promise = new Promise((resolve, reject) => {
          this.itemService.fetchChildNodes(this.mapping.Target.DatabaseName,node.id)
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
  fetchTemplates(){
    if(this.mapping && this.mapping.Target && this.mapping.Target.DatabaseName){
      this.isLoading = true;
      this.isErrorResponse = false;
      this.itemService.fetchTemplates(this.mapping.Target.DatabaseName).subscribe({
        next: data => {
          this.targetTemplates = data["data"] as Template[];
          this.isLoading = false;
          this.messages = data["messages"];    
          this.loadService.filter('Mapping Loaded');  
        },
        error: error => {
          this.targetTemplates = [];
          this.isErrorResponse = true;
          this.isLoading = false;
        }
      });
    }
  }
  fetchDatabases(){
    this.isLoading = true;
    this.isErrorResponse = false;
    this.itemService.fetchDatabases().subscribe({
      next: data => {
        this.databases = data["data"] as Database[];
        this.isLoading = false;
        this.messages = data["messages"];
        this.fetchTemplates();
        this.fetchSitecoreTree();
      },
      error: error => {
        this.databases = [];
        this.isErrorResponse = true;
        this.isLoading = false;
      }
    });
  }
  fetchSitecoreTree() {
    if(this.mapping && this.mapping.Target && this.mapping.Target.FullPath)
    {
        var expandedIds = this.mapping.Target.FullPath.trim().split("/");
        this.activeId = expandedIds.pop();
    }
    if(this.mapping.Target.DatabaseName){
      this.isLoading = true;
      this.isErrorResponse = false;
      this.itemService.fetchSitecoreTree(this.mapping.Target.DatabaseName,this.mapping.Target.FullPath).subscribe({
        next: data => {
          this.sitecoreTree = data["data"] as TreeNode[];
          this.isLoading = false;
          this.messages = data["messages"]; 
          this.cd.detectChanges();   
        },
        error: error => {
          this.isErrorResponse = true;
          this.isLoading = false;
        }
      });
    }
  }
  loadTemplates(){
    this.fetchTemplates();
    this.fetchSitecoreTree();
  }

  onActivate(evt){
    if(evt.node){
      this.mapping.Target.ItemPath = evt.node.data.path;
      this.mapping.Target.FullPath = evt.node.data.longId;
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
}
