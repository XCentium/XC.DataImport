export interface FieldMapping{
    SourceFields:string;
    IsId:boolean;
    ProcessingScripts: string[];
    TargetFields:string;
    ReferenceItemsField:string;
    ReferenceItemsTemplate:string;
    Exclude:boolean;
    Overwrite:boolean;
}