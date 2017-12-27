import { Component, OnInit, ComponentFactoryResolver } from '@angular/core';
import { SciLogoutService } from '@speak/ng-sc/logout';
import { SciAuthService } from '@speak/ng-sc/auth';
import { ItemService } from '../item.service';
import { ActivatedRoute } from '@angular/router';
import { BatchMapping } from '../create-batch-mapping/models/batchmapping';

@Component({
  selector: 'app-run-batch-mapping',
  templateUrl: './run-batch-mapping.component.html',
  styleUrls: ['./run-batch-mapping.component.scss']
})
export class RunBatchMappingComponent implements OnInit {
  interval: NodeJS.Timer;  
  isImportRunning: boolean;
  results: any;
  messages: string[];
  mapping: BatchMapping = {} as BatchMapping;
  isErrorResponse: boolean;
  isLoading: boolean;
  mappingId: any;
  isNavigationShown:boolean = true;
  
  constructor(
    public authService: SciAuthService,
    public logoutService: SciLogoutService,
    public itemService: ItemService,
    private route: ActivatedRoute,
    private componentFactoryResolver: ComponentFactoryResolver
  ) { }

  ngOnInit() {
    this.mappingId = this.route.snapshot.params["mappingId"];
    this.fetchBatchMapping();   
  }
  fetchBatchMapping() {
    this.isLoading = true;
    this.isErrorResponse = false;
    if(this.mappingId != "-") {
      this.itemService.fetchBatchMapping(this.mappingId).subscribe({
        next: data => {
          this.mapping = data["data"] as BatchMapping;
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


  runImport(){
    this.isImportRunning = true;
    this.messages = [];
    this.itemService.startBatchImport(this.mappingId).subscribe({
      next: data => {
        this.interval = setInterval(() => {
          this.fetchBatchImportResults();
        }, 3000);
      },
      error: error => {
        this.isErrorResponse = true;
        this.isLoading = false;
        this.messages.push("Mapping wasn't found");
      }
    });     
  }

  fetchBatchImportResults(): any {
    this.isLoading = true;
    this.isErrorResponse = false;
    this.itemService.fetchBatchImportResults(this.mappingId).subscribe({
      next: data => {
        this.results = data;
        this.isLoading = false;        
        if(this.results.indexOf("Import is done") > -1 || this.results.indexOf("BATCH IMPORT IS DONE") > -1){
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
}
