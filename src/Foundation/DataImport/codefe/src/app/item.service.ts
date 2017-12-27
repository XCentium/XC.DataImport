import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http'; 

@Injectable()
export class ItemService {

  constructor(private http: HttpClient) { }

  fetchDatabases() {
    return this.http.get(`/sitecore/api/ssc/dataimport/mappings/-/getdatabases`);
  }
  fetchConnectionStrings() {
    return this.http.get(`/sitecore/api/ssc/dataimport/mappings/-/getconnectionstrings`);
  }
  fetchMappings() {
    return this.http.get(`/sitecore/api/ssc/dataimport/mappings/-/getmappings`);
  }
  fetchBatchMappings() {
    return this.http.get(`/sitecore/api/ssc/dataimport/mappings/-/getbatchmappings`);
  }
  fetchMapping(id){
    if(!id){
      id="-";
    }
    return this.http.get("/sitecore/api/ssc/dataimport/mappings/"+id+"/getmapping");
  }
  fetchBatchMapping(id){
    if(!id){
      id="-";
    }
    return this.http.get("/sitecore/api/ssc/dataimport/mappings/"+id+"/getbatchmapping");
  }
  saveMapping(mapping){
    return this.http.post("/sitecore/api/ssc/dataimport/mappings/-/editmapping", mapping);
  }
  saveBatchMapping(mapping){
    return this.http.post("/sitecore/api/ssc/dataimport/mappings/-/editbatchmapping", mapping);
  }
  fetchSourceTypes(){
    return this.http.get(`/sitecore/api/ssc/dataimport/mappings/-/getsourcetypes`);
  }
  fetchTemplates(database){
    return this.http.get("/sitecore/api/ssc/dataimport/mappings/"+database+"/gettemplates");
  }
  fetchSitecoreTree(database,id){
    if(!id){
      return this.http.get("/sitecore/api/ssc/dataimport/mappings/"+database+"/getsitecoretree");
    }
    return this.http.get("/sitecore/api/ssc/dataimport/mappings/"+database+"/getsitecoretree?itemId=" + id);
  }
  fetchSitecoreTreeOptions(baseurl,id){
    if(!id){
      return this.http.get(baseurl);
    }
    return this.http.get(baseurl+"?itemId=" + id);
  }
  fetchChildNodes(database,id){
    return this.http.get("/sitecore/api/ssc/dataimport/mappings/"+database+"/getsitecoretreechildnodes?itemId=" + id);
  }
  fetchFields(database,id){
    return this.http.get("/sitecore/api/ssc/dataimport/mappings/"+database+"/getfields?fieldId=" + id);
  }
  deleteMapping(id) {
    if(!id){
      id="-";
    }
    return this.http.get("/sitecore/api/ssc/dataimport/mappings/"+id+"/deletemapping");
  }
  fetchImportResults(id: any): any {
    return this.http.get("/sitecore/api/ssc/dataimport/run/"+id+"/getimportresults");
  }
  fetchBatchImportResults(id: any): any {
    return this.http.get("/sitecore/api/ssc/dataimport/run/"+id+"/getbatchimportresults");
  }
  startImport(id: any): any {
    return this.http.get("/sitecore/api/ssc/dataimport/run/"+id+"/startimport");
  }
  startBatchImport(id: any): any {
    return this.http.get("/sitecore/api/ssc/dataimport/run/"+id+"/startbatchimport");
  }
  fetchSourceOptions(url: string): any {
    return this.http.get(url);
  }
  fetchTargetTypes(): any {
    return this.http.get(`/sitecore/api/ssc/dataimport/mappings/-/gettargettypes`);
  }
}
