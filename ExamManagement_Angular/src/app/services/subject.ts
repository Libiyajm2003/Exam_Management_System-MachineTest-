import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ISubject } from '../models/ISubject';


const API_BASE = 'http://localhost:5000/api';


@Injectable({ providedIn: 'root' })
export class SubjectService {
private url = `${API_BASE}/subjects`;
constructor(private http: HttpClient) {}


getAll(): Observable<ISubject[]> {
return this.http.get<ISubject[]>(this.url);
}


getById(id: number): Observable<ISubject> {
return this.http.get<ISubject>(`${this.url}/${id}`);
}


create(sub: Partial<ISubject>): Observable<ISubject> {
return this.http.post<ISubject>(this.url, sub);
}


update(id: number, sub: Partial<ISubject>): Observable<ISubject> {
return this.http.put<ISubject>(`${this.url}/${id}`, sub);
}


delete(id: number) {
return this.http.delete(`${this.url}/${id}`);
}
}