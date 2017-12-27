import { Component, OnInit } from '@angular/core';
import { SciAuthService } from '@speak/ng-sc/auth';
import { SciLogoutService } from '@speak/ng-sc/logout';
import { ItemService } from '../item.service';
import { ActivatedRoute } from '@angular/router';
import { BatchMapping } from './models/batchmapping';
import { MappingItem } from '../existing-mappings/mapping-item';
import { MappingReference } from './models/mappingReference';

@Component({
  selector: 'app-create-batch-mapping',
  templateUrl: './create-batch-mapping.component.html',
  styleUrls: ['./create-batch-mapping.component.scss']
})
export class CreateBatchMappingComponent implements OnInit {
  mappings: MappingItem[];
  messages: string[] = [];
  isErrorResponse: boolean;
  isLoading: boolean;
  mapping: BatchMapping = { 
    Mappings: [] as MappingReference[],
    RunInParallel: false
  } as BatchMapping;
  mappingId: any;
  isNavigationShown:boolean = true;

  constructor(
    public authService: SciAuthService,
    public logoutService: SciLogoutService,
    public itemService: ItemService,
    private route: ActivatedRoute) { }

  ngOnInit() {
    this.mappingId = this.route.snapshot.params["mappingId"];
    if(this.mappingId == "-"){
      this.mapping =  { } as BatchMapping;
      this.fetchMappings();
    } else{
      this.fetchBatchMapping();    
    }    
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
          this.fetchMappings();     
        },
        error: error => {
          this.isErrorResponse = true;
          this.isLoading = false;
          this.messages.push("Mapping wasn't found");
        }
      });  
    }   
  }
  fetchMappings(){
    this.isLoading = true;
    this.isErrorResponse = false;
    this.itemService.fetchMappings().subscribe({
      next: data => {
        this.mappings = data["data"] as MappingItem[];
        this.isLoading = false;
        this.messages = data["messages"];
      },
      error: error => {
        this.isErrorResponse = true;
        this.isLoading = false;
      }
    });  
  }
  updateCheckedOptions(mappingItem: MappingItem, event) {
    if(this.mapping){
      var match = -1;
      if(this.mapping.Mappings) {
        match = this.mapping.Mappings.findIndex(i=>i.Id === mappingItem.Id);
      } else{
        this.mapping.Mappings = [] as MappingReference[];
      }
      if(!event.target.checked && match > -1){
        this.mapping.Mappings.splice(match,1);
      } 
      else if(event.target.checked && match === -1){        
        this.mapping.Mappings.push({ Id: mappingItem.Id, Name: mappingItem.Name } as MappingReference);
      }
    }
 }
 isChecked(id:string){
  var match = -1;
  if(this.mapping.Mappings) {
    match = this.mapping.Mappings.findIndex(i=>i.Id === id);
  }
  return match >= 0;
 }  
 
 saveMapping() {
    this.messages = [];
    this.itemService.saveBatchMapping(this.mapping).subscribe({
      next: data => {
        this.mapping = data["data"] as BatchMapping;
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
}
