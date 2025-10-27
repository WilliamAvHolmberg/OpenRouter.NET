declare module 'sql.js' {
  interface QueryExecResult {
    columns: string[];
    values: any[][];
  }

  interface Database {
    run(sql: string, params?: any[]): void;
    exec(sql: string, params?: any[]): QueryExecResult[];
    prepare(sql: string): Statement;
    export(): Uint8Array;
    close(): void;
  }

  interface Statement {
    step(): boolean;
    get(params?: any[]): any[];
    getColumnNames(): string[];
    getAsObject(params?: any[]): any;
    bind(values?: any[]): boolean;
    reset(): void;
    free(): void;
    run(params?: any[]): void;
  }

  interface SqlJsStatic {
    Database: new (data?: ArrayLike<number> | Buffer | null) => Database;
  }

  interface Config {
    locateFile?: (filename: string, prefix?: string) => string;
  }

  export default function initSqlJs(config?: Config): Promise<SqlJsStatic>;
}
