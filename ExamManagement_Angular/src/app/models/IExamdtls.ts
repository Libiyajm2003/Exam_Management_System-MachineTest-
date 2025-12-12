import { ISubject } from './ISubject';

export interface IExamDtls {
dtlsID?: number;
masterID?: number;
subjectID: number;
marks: number;
subject?: ISubject;
}