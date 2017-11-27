import { Component, OnInit, Input } from '@angular/core';
import { Mapping } from '../edit-mapping-page/models/mapping';
import { SciAuthService } from '@speak/ng-sc/auth';
import { SciLogoutService } from '@speak/ng-sc/logout';
import { ItemService } from '../item.service';
import { ActivatedRoute } from '@angular/router';
import { Database } from '../edit-mapping-page/models/database';
import { Template } from '../edit-mapping-page/models/template';
import { LoadService } from '../edit-mapping-page/load.service';
import { TreeNode } from 'angular-tree-component';

@Component({
  selector: 'xc-target-system',
  templateUrl: './target-system.component.html',
  styleUrls: ['./target-system.component.scss']
})
export class TargetSystemComponent implements OnInit {
  targetTemplates: any[];
  databases: any[];
  sitecoreTreeState: any;
  messages: any;
  sitecoreTree: any;
  isErrorResponse: boolean;
  isLoading: boolean;
  sitecoreTreeOptions: {};

  @Input() mapping: Mapping = {} as Mapping;

  constructor(public authService: SciAuthService,
    public logoutService: SciLogoutService,
    public itemService: ItemService,
    private route: ActivatedRoute,
    private loadService: LoadService) 
  {
    this.sitecoreTreeOptions = {
      getChildren: (node:TreeNode) => {
        return this.fetchSitecoreTree(node.id);
      }
    }
  }

  ngOnInit() {
    this.fetchDatabases();    
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
        this.fetchSitecoreTree("");
      },
      error: error => {
        this.databases = [];
        this.isErrorResponse = true;
        this.isLoading = false;
      }
    });
  }
  fetchSitecoreTree(id) {
    if(this.mapping.Target.DatabaseName){
      this.isLoading = true;
      this.isErrorResponse = false;
      this.itemService.fetchSitecoreTree(this.mapping.Target.DatabaseName,id).subscribe({
        next: data => {
          this.sitecoreTree = data["data"];
          this.isLoading = false;
          this.messages = data["messages"];
          this.sitecoreTreeState = {
            ...this.sitecoreTreeState,
            expandedNodeIds: { id },
            focusedNodeId: id 
          };
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
    this.fetchSitecoreTree("");
  }
}
