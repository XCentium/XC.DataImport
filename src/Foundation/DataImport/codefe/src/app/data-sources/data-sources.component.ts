import { Component, OnInit, Input } from '@angular/core';
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

@Component({
  selector: 'xc-datasources',
  templateUrl: './data-sources.component.html',
  styleUrls: ['./data-sources.component.scss']
})
export class DataSourcesComponent implements OnInit {
  @Input() mapping: Mapping = {} as Mapping;

  messages: any;
  sourceTypes: any[];
  isErrorResponse: boolean;
  isLoading: boolean;
  mappingId:string = '';
  sourceType:string = '';
  fileInputVisible:boolean = true;

  testConfig: Ng4FilesConfig = {
    acceptExtensions: ['csv', 'xsl', 'txt'],
    maxFilesCount: 1,
    maxFileSize: 5120000,
    totalFilesSize: 10120000
  };

  constructor(
    public authService: SciAuthService,
    public logoutService: SciLogoutService,
    public itemService: ItemService,
    private route: ActivatedRoute
  ) { }

  ngOnInit() {
    this.fetchSourceTypes();
    if(this.mapping.Source  as FileDataSource) {
      if(this.mapping.Source as FileDataSource && !(this.mapping.Source as FileDataSource).FilePath){
        this.fileInputVisible = false;
      } 
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
      },
      error: error => {
        this.sourceTypes = [];
        this.isErrorResponse = true;
        this.isLoading = false;
      }
    });
  }

  showSource(sourceSelection){
    this.sourceType = sourceSelection.selectedOptions[0].label;
  }

  fileChangeEvent(fileInput){
    if (fileInput.target.files && fileInput.target.files[0]) {
      this.readFileInputEntry(fileInput.target.files[0]);
    }
  }
  showFileInput(){
    this.fileInputVisible = true;
  }
  readFileInputEntry(file)
  {
    var reader = new FileReader();    
    var self = this;
    reader.onload = function (e : any) {
      self.uploadFileInputEntry(e.target.result);
    }
    reader.readAsDataURL(file);
  }
  uploadFileInputEntry(fileInputEntry){
    this.isLoading = true;
    this.isErrorResponse = false;
    this.itemService.uploadFile(fileInputEntry).subscribe({
      next: data => {
        if(this.mapping.Source as FileDataSource){
          (this.mapping.Source as FileDataSource).FilePath = data["data"] as string;
        } 
      },
      error: error => {
        this.isErrorResponse = true;
        this.isLoading = false;
      }
    });    
  }
}
