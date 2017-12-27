import { MappingReference } from "./mappingReference";

export interface BatchMapping {
    Id:string,
    Name:string,
    Mappings: MappingReference[],
    RunInParallel:boolean
}