import { Component, OnInit, ComponentFactoryResolver, ViewChild, AfterViewInit, OnDestroy } from '@angular/core';
import { SciAuthService } from '@speak/ng-sc/auth';
import { SciLogoutService } from '@speak/ng-sc/logout';
import { ItemService } from '../item.service';
import { ActivatedRoute } from '@angular/router';
import { Mapping } from '../edit-mapping-page/models/mapping';
import { ResultsDirective } from './results.directive';
import { retry } from 'rxjs/operator/retry';

@Component({
  selector: 'app-run-mapping-page',
  templateUrl: './run-mapping-page.component.html',
  styleUrls: ['./run-mapping-page.component.scss']
})
export class RunMappingPageComponent implements OnInit,  AfterViewInit, OnDestroy {
  interval: NodeJS.Timer;  
  messages: any;
  mapping: Mapping;
  isErrorResponse: boolean;
  isLoading: boolean;
  mappingId: string;
  results:string = "";
  isNavigationShown:boolean = true;
  isImportRunning:boolean = false;

  @ViewChild(ResultsDirective) resultsHost: ResultsDirective;

  constructor(
    public authService: SciAuthService,
    public logoutService: SciLogoutService,
    public itemService: ItemService,
    private route: ActivatedRoute,
    private componentFactoryResolver: ComponentFactoryResolver
  ) { }

  ngAfterViewInit() {
    
  }

  ngOnInit() {
    // get param
    this.mappingId = this.route.snapshot.params["mappingId"];
    this.fetchMapping();    
  }
  ngOnDestroy() {
    clearInterval(this.interval);
  }

  fetchMapping() {
    this.isLoading = true;
    this.isErrorResponse = false;
    this.itemService.fetchMapping(this.mappingId).subscribe({
      next: data => {
        this.mapping = data["data"] as Mapping;
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

  runImport(){
    this.isImportRunning = true;
    this.messages = [];
    this.itemService.startImport(this.mappingId).subscribe({
      next: data => {
        this.interval = setInterval(() => {
          this.fetchImportResults();
        }, 3000);
      },
      error: error => {
        this.isErrorResponse = true;
        this.isLoading = false;
        this.messages.push("Mapping wasn't found");
      }
    });     
  }

  fetchImportResults(): any {
    this.isLoading = true;
    this.isErrorResponse = false;
    this.itemService.fetchImportResults(this.mappingId).subscribe({
      next: data => {
        this.results = data;
        this.isLoading = false;        
        if(this.results.indexOf("Import is done") > -1 || this.results.indexOf("[DONE]") > -1){
          clearInterval(this.interval);
          this.isImportRunning = false;
        }
      },
      error: error => {
        this.isErrorResponse = true;
        this.isLoading = false;
        this.messages.push("Mapping wasn't found");
      }
    }); 
  }
  objectKeys(obj) {
    if(obj){
      return Object.keys(obj);
    }
    return [];
  }
}
