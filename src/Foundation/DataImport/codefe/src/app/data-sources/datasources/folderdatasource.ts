import { Source } from "./source";

export interface FolderDataSource extends Source{
    FolderPath:string;
    FilePattern:string;
  }