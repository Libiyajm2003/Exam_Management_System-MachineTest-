import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { IExamMaster } from '../models/IExamMaster';


const API_BASE = 'http://localhost:5000/api';


@Injectable({ providedIn: 'root' })
export class ExamMasterService {
private url = `${API_BASE}/exams`;
constructor(private http: HttpClient) {}


getAll(): Observable<IExamMaster[]> {
return this.http.get<IExamMaster[]>(this.url);
}


getById(id: number): Observable<IExamMaster> {
return this.http.get<IExamMaster>(`${this.url}/${id}`);
}


create(request: any): Observable<IExamMaster> {
return this.http.post<IExamMaster>(this.url, request);
}


// optionally add search by student/year
getByStudentAndYear(studentId: number, year: number): Observable<IExamMaster[]> {
return this.http.get<IExamMaster[]>(`${this.url}?studentId=${studentId}&year=${year}`);
}
}