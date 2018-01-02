import { SourceType } from "./sourcetype";
import { Target } from "./target";
import { FieldMapping } from "./FieldMapping";
import { Source } from "../../data-sources/datasources/source";

export interface Mapping {
    Id:string,
    Name: string,
    Source: Source,
    SourceType: SourceType,
    SourceProcessingScripts: string[],
    Target: Target,
    PostImportProcessingScripts: string[],
    FieldMappings: FieldMapping[],
    TargetType: SourceType,
    ExcludeFieldMappingFields: boolean
  }


  
  