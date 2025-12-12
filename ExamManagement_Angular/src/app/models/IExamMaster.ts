import { IExamDtls } from './IExamdtls';
import { IStudent } from './IStudent';


export interface IExamMaster {
masterID?: number;
studentID: number;
examYear: number;
totalMark?: number;
passOrFail?: string;
createTime?: string;
details?: IExamDtls[];
student?: IStudent;
}