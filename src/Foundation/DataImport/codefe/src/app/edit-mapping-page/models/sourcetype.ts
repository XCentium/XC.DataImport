import { InputField } from "./inputfield";

export interface SourceType {
    Name: string;
    ModelType: string;
    DataSourceType: string;
    Fields: InputField[]
  }