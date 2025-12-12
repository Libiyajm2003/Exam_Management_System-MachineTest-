import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { IStudent } from '../models/IStudent';


const API_BASE = 'http://localhost:5000/api';


@Injectable({ providedIn: 'root' })
export class StudentService {
private url = `${API_BASE}/students`;
constructor(private http: HttpClient) {}


getAll(): Observable<IStudent[]> {
return this.http.get<IStudent[]>(this.url);
}


getById(id: number): Observable<IStudent> {
return this.http.get<IStudent>(`${this.url}/${id}`);
}


create(student: Partial<IStudent>): Observable<IStudent> {
return this.http.post<IStudent>(this.url, student);
}


update(id: number, student: Partial<IStudent>): Observable<IStudent> {
return this.http.put<IStudent>(`${this.url}/${id}`, student);
}


delete(id: number) {
return this.http.delete(`${this.url}/${id}`);
}
}