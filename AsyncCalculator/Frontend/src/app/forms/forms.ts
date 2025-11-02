import { Component } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-forms',
  standalone: true,
  templateUrl: './forms.html',
  styleUrl: './forms.css',
  imports: [ReactiveFormsModule, HttpClientModule],
})
export class Forms {
  number1: FormControl = new FormControl('');
  number2: FormControl = new FormControl('');
  result: FormControl = new FormControl('');

  isLoading: boolean = false;

  constructor(private http: HttpClient) { };

  

  async updateResult() {
    let number1: number = parseInt(this.number1.getRawValue() || 0);
    let number2: number = parseInt(this.number2.getRawValue() || 0);
    let id: string;

    const payload: object = { n1: number1, n2: number2 };

    try {
      const postRaw = await this.http
        .post('http://localhost:1337/CalcAsync', payload, { responseType: 'text' })
        .toPromise();

      let postResponse: { messageId: string };
      try {
        postResponse = JSON.parse(postRaw ?? "");
      } catch {
        console.error('Failed to parse POST response', postRaw);
        throw new Error('Invalid POST response format');
      }

      const id: string = postResponse.messageId;
      if (!id) {
        throw new Error('messageId missing from POST response');
      }
      console.log('POST messageId:', id);

      this.isLoading = true;

      const getResponse = await this.http
        .get<{ result: string}>(`http://localhost:1337/GetResult/${id}`)
        .toPromise();

      console.log(`GET response: ${JSON.stringify(getResponse)}`);
      this.isLoading = false;
      this.result.setValue(getResponse?.result);

    } catch (err) {
      console.log(`error: ${err}`);
    }
  }

}
