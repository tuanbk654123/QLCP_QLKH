import React from 'react';
import moment from 'moment';
import './ContractTemplate.css';

const ContractTemplate = ({ contract }) => {
  if (!contract) return null;

  const formatDate = (date) => {
    if (!date) return '';
    return moment(date).format('DD/MM/YYYY');
  };

  const formatCurrency = (amount) => {
    if (!amount) return '';
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
    }).format(amount);
  };

  const renderTerms = () => {
    if (!contract.terms) return null;
    if (Array.isArray(contract.terms)) {
      return contract.terms.map((term, index) => (
        <p key={index} className="contract-term">
          {index + 1}. {term}
        </p>
      ));
    }
    if (typeof contract.terms === 'string') {
      return contract.terms.split('\n').map((term, index) => (
        <p key={index} className="contract-term">
          {index + 1}. {term}
        </p>
      ));
    }
    return null;
  };

  return (
    <div className="contract-template">
      <div className="contract-header">
        <h1 className="contract-title">CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</h1>
        <p className="contract-subtitle">Độc lập - Tự do - Hạnh phúc</p>
        <div className="contract-divider"></div>
        <h2 className="contract-type">{contract.contractType?.toUpperCase() || 'HỢP ĐỒNG'}</h2>
        <p className="contract-number">Số: {contract.contractNumber || 'HD-2024-XXX'}</p>
      </div>

      <div className="contract-body">
        <div className="contract-section">
          <p className="contract-intro">
            <strong>Hôm nay, ngày {formatDate(contract.contractDate) || '__/__/____'}</strong>
          </p>
          <p className="contract-parties">
            <strong>BÊN A (Bên thuê/Bên mua):</strong>
          </p>
          <div className="contract-party-details">
            <p><strong>Tên:</strong> {contract.partyA?.name || '_________________'}</p>
            {contract.partyA?.taxCode && (
              <p><strong>Mã số thuế:</strong> {contract.partyA.taxCode}</p>
            )}
            {contract.partyA?.address && (
              <p><strong>Địa chỉ:</strong> {contract.partyA.address}</p>
            )}
            {contract.partyA?.representative && (
              <p><strong>Người đại diện:</strong> {contract.partyA.representative}</p>
            )}
            {contract.partyA?.position && (
              <p><strong>Chức vụ:</strong> {contract.partyA.position}</p>
            )}
            {contract.partyA?.phone && (
              <p><strong>Điện thoại:</strong> {contract.partyA.phone}</p>
            )}
            {contract.partyA?.email && (
              <p><strong>Email:</strong> {contract.partyA.email}</p>
            )}
          </div>

          <p className="contract-parties" style={{ marginTop: '20px' }}>
            <strong>BÊN B (Bên được thuê/Bên bán):</strong>
          </p>
          <div className="contract-party-details">
            <p><strong>Tên:</strong> {contract.partyB?.name || '_________________'}</p>
            {contract.partyB?.idCard && (
              <p><strong>CMND/CCCD/Mã số thuế:</strong> {contract.partyB.idCard}</p>
            )}
            {contract.partyB?.address && (
              <p><strong>Địa chỉ:</strong> {contract.partyB.address}</p>
            )}
            {contract.partyB?.position && (
              <p><strong>Chức vụ/Vị trí:</strong> {contract.partyB.position}</p>
            )}
            {contract.partyB?.phone && (
              <p><strong>Điện thoại:</strong> {contract.partyB.phone}</p>
            )}
            {contract.partyB?.email && (
              <p><strong>Email:</strong> {contract.partyB.email}</p>
            )}
          </div>

          <p className="contract-agreement">
            Hai bên thỏa thuận ký kết hợp đồng với các điều khoản sau:
          </p>
        </div>

        <div className="contract-section">
          <h3 className="contract-clause-title">ĐIỀU 1: ĐỐI TƯỢNG HỢP ĐỒNG</h3>
          {contract.contractType === 'Hợp đồng lao động' && (
            <div>
              <p>Bên A thuê Bên B làm việc tại {contract.partyA?.name || 'công ty'} với vị trí {contract.partyB?.position || '_________________'}.</p>
              {contract.jobDescription && (
                <p><strong>Mô tả công việc:</strong> {contract.jobDescription}</p>
              )}
              {contract.workingHours && (
                <p><strong>Thời gian làm việc:</strong> {contract.workingHours}</p>
              )}
              {contract.salary && (
                <p><strong>Mức lương:</strong> {formatCurrency(contract.salary)}/tháng</p>
              )}
            </div>
          )}
          {contract.contractType === 'Hợp đồng dịch vụ' && (
            <div>
              <p>Bên B cung cấp dịch vụ cho Bên A với nội dung:</p>
              {contract.serviceDescription && (
                <p>{contract.serviceDescription}</p>
              )}
              {contract.serviceFee && (
                <p><strong>Phí dịch vụ:</strong> {formatCurrency(contract.serviceFee)}</p>
              )}
              {contract.paymentTerms && (
                <p><strong>Điều kiện thanh toán:</strong> {contract.paymentTerms}</p>
              )}
            </div>
          )}
          {contract.contractType === 'Hợp đồng mua bán' && (
            <div>
              <p>Bên B bán cho Bên A:</p>
              {contract.productDescription && (
                <p>{contract.productDescription}</p>
              )}
              {contract.totalAmount && (
                <p><strong>Tổng giá trị:</strong> {formatCurrency(contract.totalAmount)}</p>
              )}
              {contract.paymentTerms && (
                <p><strong>Điều kiện thanh toán:</strong> {contract.paymentTerms}</p>
              )}
              {contract.deliveryTerms && (
                <p><strong>Điều kiện giao hàng:</strong> {contract.deliveryTerms}</p>
              )}
            </div>
          )}
        </div>

        <div className="contract-section">
          <h3 className="contract-clause-title">ĐIỀU 2: THỜI HẠN HỢP ĐỒNG</h3>
          <p>
            Hợp đồng có hiệu lực từ ngày <strong>{formatDate(contract.effectiveDate) || '__/__/____'}</strong>
            {contract.expiryDate && (
              <> đến ngày <strong>{formatDate(contract.expiryDate)}</strong></>
            )}
            .
          </p>
        </div>

        <div className="contract-section">
          <h3 className="contract-clause-title">ĐIỀU 3: QUYỀN VÀ NGHĨA VỤ</h3>
          {renderTerms()}
        </div>

        <div className="contract-section">
          <h3 className="contract-clause-title">ĐIỀU 4: ĐIỀU KHOẢN CHUNG</h3>
          <p className="contract-term">1. Hợp đồng này được lập thành 02 (hai) bản, mỗi bên giữ 01 (một) bản có giá trị pháp lý như nhau.</p>
          <p className="contract-term">2. Mọi tranh chấp phát sinh từ hợp đồng này sẽ được giải quyết thông qua thương lượng. Nếu không thương lượng được, sẽ đưa ra Tòa án nhân dân có thẩm quyền giải quyết.</p>
          <p className="contract-term">3. Hợp đồng này có hiệu lực kể từ ngày ký.</p>
        </div>

        {contract.notes && (
          <div className="contract-section">
            <h3 className="contract-clause-title">GHI CHÚ</h3>
            <p>{contract.notes}</p>
          </div>
        )}

        <div className="contract-signature">
          <div className="signature-box">
            <p className="signature-title"><strong>BÊN A</strong></p>
            <p className="signature-name">{contract.partyA?.representative || '_________________'}</p>
            <p className="signature-position">{contract.partyA?.position || ''}</p>
          </div>
          <div className="signature-box">
            <p className="signature-title"><strong>BÊN B</strong></p>
            <p className="signature-name">{contract.partyB?.name || '_________________'}</p>
            <p className="signature-position">{contract.partyB?.position || ''}</p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ContractTemplate;

