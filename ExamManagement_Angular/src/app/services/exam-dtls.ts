import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { IExamDtls } from '../models/IExamdtls';


const API_BASE = 'http://localhost:5000/api';


@Injectable({ providedIn: 'root' })
export class ExamDtlsService {
private url = `${API_BASE}/examdtls`;
constructor(private http: HttpClient) {}


getByMaster(masterId: number): Observable<IExamDtls[]> {
return this.http.get<IExamDtls[]>(`${this.url}/bymaster/${masterId}`);
}


createMany(details: Partial<IExamDtls>[]): Observable<any> {
return this.http.post(`${this.url}/batch`, details);
}
}